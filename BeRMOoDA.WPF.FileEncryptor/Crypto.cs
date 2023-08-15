using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace BeRMOoDA.WPF.FileEncryptor
{
    public class FileCrypto
    {
        public int BufferSize { get; set; }
        public CryptoLevel CryptoLevel { get; set; }
        public delegate void EncryptionProgressHandler(object sender, CryptoEventArgs e);
        public event EncryptionProgressHandler EncryptionProgress;
        public event EncryptionProgressHandler DecryptionProgress;
        public event EncryptionProgressHandler DecryptionFinished;
        public event EncryptionProgressHandler EncryptionFinished;
        public FileCrypto(int bufferSize, CryptoLevel cryptoLevel)
        {
            this.BufferSize = bufferSize;
            this.CryptoLevel = cryptoLevel;
        }

        public FileCrypto()
            : this(256, CryptoLevel.Normal)
        {

        }


        public void EncryptFile(string file, string password)
        {
            if (password == null)
                throw new ArgumentNullException("password can not be null");
            StringBuilder s = new StringBuilder();
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, Encoding.UTF8.GetBytes("Salt"));
            byte[] key = pdb.GetBytes(16);
            byte[] IV = pdb.GetBytes(16);
            long position = 0;
            FileStream fStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            byte[] buffer = new byte[BufferSize];
            while (fStream.Position + BufferSize < fStream.Length)
            {
                position = fStream.Position;
                fStream.Read(buffer, 0, BufferSize);
                fStream.Position = position;
                byte[] encryptedData = EncryptData(buffer, key, IV, PaddingMode.Zeros);
                foreach (var item in encryptedData)
                {
                    s.Append(item);
                    s.Append(',');
                }
                fStream.Write(encryptedData, 0, encryptedData.Length);
                fStream.Flush();
                s.Remove(0, s.Length);
                 EncryptionProgress(this, new CryptoEventArgs() { BufferSize=this.BufferSize, Percentage = (double)((double)fStream.Position / (double)fStream.Length) * 100d, CurrentFileSize = fStream.Length });
            }
            buffer = new byte[(int)(fStream.Length - fStream.Position)];
            position = fStream.Position;
            fStream.Read(buffer, 0, buffer.Length);
            fStream.Position = position;
            byte[] encryptedData2 = EncryptData(buffer, key, IV, PaddingMode.Zeros);
            foreach (var item in encryptedData2)
            {
                s.Append(item);
                s.Append(',');
            }
            s.Remove(s.Length - 1, 1);
            fStream.Write(encryptedData2, 0, encryptedData2.Length);
            fStream.Flush();
            EncryptionProgress(this, new CryptoEventArgs() { BufferSize = this.BufferSize, Percentage = (double)((double)fStream.Position / (double)fStream.Length) * 100d, CurrentFileSize = fStream.Length });
            MD5 md5 = MD5.Create();
            byte[] passHash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            fStream.Write(passHash, 0, passHash.Length);
            fStream.Flush();
            s.Remove(0, s.Length);
            EncryptionFinished(this, new CryptoEventArgs() { BufferSize = this.BufferSize, Percentage = 100, CurrentFileSize = fStream.Length });
            fStream.Close();

        }

        public void DecryptFile(string file, string password)
        {
            if (password == null)
                throw new ArgumentNullException("password  can not be null");
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, Encoding.UTF8.GetBytes("Salt"));

            byte[] key = pdb.GetBytes(16);
            byte[] IV = pdb.GetBytes(16);
            long position = 0;
            FileStream fStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            MD5 md5 = MD5.Create();
            byte[] passHash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            fStream.Position = fStream.Length - 16;
            byte[] oldPassHash = new byte[16];
            fStream.Read(oldPassHash, 0, oldPassHash.Length);
            if (Encoding.UTF8.GetString(passHash) != Encoding.UTF8.GetString(oldPassHash))
                throw new InvalidPasswordException();
            fStream.SetLength(fStream.Length - 16);
            fStream.Position = 0;
            byte[] buffer = new byte[BufferSize];
            while (fStream.Position + BufferSize < fStream.Length)
            {
                position = fStream.Position;
                fStream.Read(buffer, 0, BufferSize);
                fStream.Position = position;
                byte[] decryptedData = DecryptData(buffer, key, IV, PaddingMode.Zeros);
                fStream.Write(decryptedData, 0, decryptedData.Length);
                fStream.Flush();
                DecryptionProgress(this, new CryptoEventArgs() { BufferSize = this.BufferSize, Percentage = (double)((double)fStream.Position / (double)fStream.Length) * 100d, CurrentFileSize = fStream.Length });
            }
            buffer = new byte[(int)(fStream.Length - fStream.Position)];
            position = fStream.Position;
            fStream.Read(buffer, 0, buffer.Length);
            fStream.Position = position;
            byte[] encryptedData = DecryptData(buffer, key, IV, PaddingMode.Zeros);
            fStream.Write(encryptedData, 0, encryptedData.Length);
            fStream.Flush();
            DecryptionProgress(this, new CryptoEventArgs() { BufferSize = this.BufferSize, Percentage = (double)((double)fStream.Position / (double)fStream.Length) * 100d, CurrentFileSize = fStream.Length });
            DecryptionFinished(this, new CryptoEventArgs() { BufferSize = this.BufferSize, Percentage = 100, CurrentFileSize = fStream.Length });
            fStream.Close();
        }

        byte[] EncryptData(byte[] data, byte[] key, byte[] IV, PaddingMode paddingMode)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException("data can not be null");
            RijndaelManaged AES = new RijndaelManaged();
            AES.Padding = paddingMode;
            //AES.KeySize = (int)CryptoLevel;

            ICryptoTransform encryptor = AES.CreateEncryptor(key, IV);
            using (MemoryStream msEncrypt = new MemoryStream())
            using (CryptoStream encStream = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                encStream.Write(data, 0, data.Length);
                encStream.FlushFinalBlock();
                return msEncrypt.ToArray();
            }
        }

        byte[] DecryptData(byte[] data, byte[] key, byte[] IV, PaddingMode paddingMode)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException("data can not be null");
            RijndaelManaged AES = new RijndaelManaged();
            AES.Padding = paddingMode;
            //AES.KeySize = (int)CryptoLevel;
            ICryptoTransform decryptor = AES.CreateDecryptor(key, IV);
            using (MemoryStream msDecrypt = new MemoryStream(data))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            {
                // Decrypted bytes will always be less then encrypted bytes, so len of encrypted data will be big enouph for buffer.
                byte[] fromEncrypt = new byte[data.Length];
                // Read as many bytes as possible.
                int read = csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);
                if (read < fromEncrypt.Length)
                {
                    // Return a byte array of proper size.
                    byte[] clearBytes = new byte[read];
                    Buffer.BlockCopy(fromEncrypt, 0, clearBytes, 0, read);
                    return clearBytes;
                }
                return fromEncrypt;
            }
        }//end fn

        public void EncryptFileOnTheFly(string file, string password)
        {
            if (password == null)
                throw new ArgumentNullException("password can not be null");

            long position = 0;
            FileStream fStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            byte[] buffer = new byte[BufferSize];
            while (fStream.Position + BufferSize < fStream.Length)
            {
                position = fStream.Position;
                fStream.Read(buffer, 0, BufferSize);
                fStream.Position = position;
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] += 100;
                }
                fStream.Write(buffer, 0, buffer.Length);
                fStream.Flush();
                EncryptionProgress(this, new CryptoEventArgs() { Percentage = (double)((double)fStream.Position / (double)fStream.Length) * 100d, CurrentFileSize = fStream.Length });
            }
            position = fStream.Position;
            buffer = new byte[Math.Abs((int)fStream.Length - (int)fStream.Position)];
            fStream.Read(buffer, 0, buffer.Length);
            fStream.Position = position;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] += 100;
            }
            fStream.Write(buffer, 0, buffer.Length);
            fStream.Flush();
            MD5 md5 = MD5.Create();
            byte[] passHash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            fStream.Write(passHash, 0, passHash.Length);
            fStream.Flush();
            EncryptionFinished(this, new CryptoEventArgs() { Percentage = 100, CurrentFileSize = fStream.Length });
            //fStream.Close();
        }


        public void DecryptFileOnTheFly(string file, string password)
        {
            if (password == null)
                throw new ArgumentNullException("password  can not be null");

            long position = 0;
            FileStream fStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            MD5 md5 = MD5.Create();
            byte[] passHash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            fStream.Position = fStream.Length - 16;
            byte[] oldPassHash = new byte[16];
            fStream.Read(oldPassHash, 0, oldPassHash.Length);
            if (Encoding.UTF8.GetString(passHash) != Encoding.UTF8.GetString(oldPassHash))
                throw new InvalidPasswordException();
            fStream.SetLength(fStream.Length - 16);
            fStream.Position = 0;
            byte[] buffer = new byte[BufferSize];
            while (fStream.Position + BufferSize <= fStream.Length)
            {
                position = fStream.Position;
                fStream.Read(buffer, 0, BufferSize);
                fStream.Position = position;
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] -= 100;
                }
                fStream.Write(buffer, 0, buffer.Length);
                fStream.Flush();
                DecryptionProgress(this, new CryptoEventArgs() { Percentage = (double)((double)fStream.Position / (double)fStream.Length) * 100d , CurrentFileSize=fStream.Length});

            }
            position = fStream.Position;
            buffer = new byte[Math.Abs((int)fStream.Length - (int)fStream.Position)];
            fStream.Read(buffer, 0, buffer.Length);
            fStream.Position = position;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] -= 100;
            }
            fStream.Write(buffer, 0, buffer.Length);
            fStream.Flush();
            DecryptionProgress(this, new CryptoEventArgs() { Percentage = (double)((double)fStream.Position / (double)fStream.Length) * 100d , CurrentFileSize=fStream.Length});
            DecryptionFinished(this, new CryptoEventArgs() { Percentage = 100 , CurrentFileSize=fStream.Length});
           // fStream.Close();
        }
    }//end class

    [global::System.Serializable]
    public class InvalidPasswordException : Exception
    {
        public InvalidPasswordException() { }
        public InvalidPasswordException(string message) : base(message) { }
        public InvalidPasswordException(string message, Exception inner) : base(message, inner) { }
        protected InvalidPasswordException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
    public enum CryptoLevel
    {
        Normal = 128, Strong = 192, VeryStrong = 256
    }

    public class CryptoEventArgs : EventArgs
    {
        public double Percentage { get; set; }
        public long CurrentFileSize { get; set; }
        public int BufferSize { get; set; }
    }
}//end namespace
