//
// MultipartEncrypted.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Jeffrey Stedfast
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;

using MimeKit.IO;
using MimeKit.IO.Filters;

namespace MimeKit.Cryptography {
	public class MultipartEncrypted : Multipart
	{
		internal MultipartEncrypted (ParserOptions options, ContentType type, IEnumerable<Header> headers, bool toplevel) : base (options, type, headers, toplevel)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MimeKit.Cryptography.MultipartEncrypted"/> class.
		/// </summary>
		public MultipartEncrypted () : base ("encrypted")
		{
		}

		/// <summary>
		/// Creates a new <see cref="MimeKit.Cryptography.MultipartEncrypted"/> instance with the entity as the content.
		/// </summary>
		/// <returns>A new <see cref="MimeKit.Cryptography.MultipartEncrypted"/> instance containing
		/// the signed and encrypted version of the specified entity.</returns>
		/// <param name="signer">The signer to use to sign the entity.</param>
		/// <param name="recipients">The recipients for the encrypted entity.</param>
		/// <param name="entity">The entity to sign and encrypt.</param>
		public static MultipartEncrypted Create (MailboxAddress signer, IEnumerable<MailboxAddress> recipients, MimeEntity entity)
		{
			using (var ctx = CryptographyContext.Create ("application/pgp-encrypted")) {
				byte[] cleartext;

				using (var memory = new MemoryStream ()) {
					using (var filtered = new FilteredStream (memory)) {
						filtered.Add (new Unix2DosFilter ());

						entity.WriteTo (filtered);
						filtered.Flush ();
					}

					cleartext = memory.ToArray ();
				}

				var encrypted = new MultipartEncrypted ();
				encrypted.ContentType.Parameters["protocol"] = ctx.EncryptionProtocol;

				// add the protocol version part
				encrypted.Add (new ApplicationPgpEncrypted ());

				// add the encrypted entity as the second part
				encrypted.Add (ctx.SignAndEncrypt (signer, recipients, cleartext));

				return encrypted;
			}
		}

		/// <summary>
		/// Creates a new <see cref="MimeKit.Cryptography.MultipartEncrypted"/> instance with the entity as the content.
		/// </summary>
		/// <returns>A new <see cref="MimeKit.Cryptography.MultipartEncrypted"/> instance containing
		/// the encrypted version of the specified entity.</returns>
		/// <param name="recipients">The recipients for the encrypted entity.</param>
		/// <param name="entity">The entity to sign and encrypt.</param>
		public static MultipartEncrypted Create (IEnumerable<MailboxAddress> recipients, MimeEntity entity)
		{
			using (var ctx = CryptographyContext.Create ("application/pgp-encrypted")) {
				byte[] cleartext;

				using (var memory = new MemoryStream ()) {
					using (var filtered = new FilteredStream (memory)) {
						filtered.Add (new Unix2DosFilter ());

						entity.WriteTo (filtered);
						filtered.Flush ();
					}

					cleartext = memory.ToArray ();
				}

				var encrypted = new MultipartEncrypted ();
				encrypted.ContentType.Parameters["protocol"] = ctx.EncryptionProtocol;

				// add the protocol version part
				encrypted.Add (new ApplicationPgpEncrypted ());

				// add the encrypted entity as the second part
				encrypted.Add (ctx.Encrypt (recipients, cleartext));

				return encrypted;
			}
		}

		/// <summary>
		/// Decrypt this instance.
		/// </summary>
		/// <returns>The decrypted entity.</returns>
		public MimeEntity Decrypt ()
		{
			var protocol = ContentType.Parameters["protocol"];
			if (string.IsNullOrEmpty (protocol))
				throw new FormatException ();

			protocol = protocol.Trim ().ToLowerInvariant ();

			if (Count < 2)
				throw new FormatException ();

			var version = this[0] as MimePart;
			if (version == null)
				throw new FormatException ();

			var ctype = version.ContentType;
			var value = string.Format ("{0}/{1}", ctype.MediaType, ctype.MediaSubtype);
			if (value.ToLowerInvariant () != protocol)
				throw new FormatException ();

			var encrypted = this[1] as MimePart;
			if (encrypted == null || encrypted.ContentObject == null)
				throw new FormatException ();

			if (!encrypted.ContentType.Matches ("application", "octet-stream"))
				throw new FormatException ();

			throw new NotSupportedException ();

//			using (var ctx = CryptographyContext.Create (protocol)) {
//				byte[] encryptedData;
//
//				using (var memory = new MemoryStream ()) {
//					encrypted.ContentObject.DecodeTo (memory);
//					encryptedData = memory.ToArray ();
//				}
//
//				return ctx.Decrypt (encryptedData, ...);
//			}
		}
	}
}