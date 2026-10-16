class GssNtlmssp < Formula
  desc "NTLM authentication plugin for GSSAPI, on Windows' SSPI NTLM package"
  homepage "https://github.com/gssapi/gss-ntlmssp"
  url "https://github.com/gssapi/gss-ntlmssp/releases/download/v1.3.2/gssntlmssp-1.3.2.tar.gz"
  sha256 "e5cc8d74e5f88cfe74622b14d1d28e85710dec898b754c2c78969f25147bbb55"
  license "ISC"

  # gss-ntlmssp is the NTLMSSP mechanism (OID 1.3.6.1.4.1.311.2.2.10) for GSSAPI mechglues, so
  # GSSAPI programs can authenticate with NTLM (HTTP Negotiate, SMB, SASL GSS-SPNEGO). Windows
  # implements NTLM itself in its SSPI NTLM package, which already does the protocol, the hashes and
  # the user's credentials (single sign-on with the logged-on user, or an explicit user/password).
  # This keg's gssntlmssp.dll is the mechanism plugin on top of it: the gssi_* entry points MIT
  # krb5's mechglue loads (names, credentials incl. acquire_cred_with_password, init/accept
  # contexts with channel bindings, MIC, wrap/unwrap, inquire), with the NTLM work done by SSPI.
  # The mechanism is configured like upstream's mech.d file, and MIT Kerberos for Windows reads it
  # from the registry: share/gss-ntlmssp/gssntlmssp.reg registers it there.

  def install
    (buildpath/"win/gssntlmssp.c").write <<~'C'
      /* gss-ntlmssp for Windows: the NTLMSSP GSSAPI mechanism (OID 1.3.6.1.4.1.311.2.2.10) as a
       * mechglue plugin, with the NTLM protocol done by Windows' own SSPI NTLM package (secur32):
       * credentials of the logged-on user or an explicit user/password, context establishment,
       * MIC, wrap/unwrap. Exports the gssi_* entry points that MIT krb5's mechglue loads. */
      #define WIN32_LEAN_AND_MEAN
      #define SECURITY_WIN32
      #include <windows.h>
      #include <sspi.h>
      #include <stdint.h>
      #include <stdlib.h>
      #include <string.h>

      typedef uint32_t OM_uint32;
      typedef struct gss_OID_desc_struct { OM_uint32 length; void *elements; } gss_OID_desc, *gss_OID;
      typedef const gss_OID_desc *gss_const_OID;
      typedef struct gss_OID_set_desc_struct { size_t count; gss_OID elements; } gss_OID_set_desc, *gss_OID_set;
      typedef struct gss_buffer_desc_struct { size_t length; void *value; } gss_buffer_desc, *gss_buffer_t;
      typedef struct gss_channel_bindings_struct {
          OM_uint32 initiator_addrtype; gss_buffer_desc initiator_address;
          OM_uint32 acceptor_addrtype; gss_buffer_desc acceptor_address;
          gss_buffer_desc application_data;
      } *gss_channel_bindings_t;
      typedef struct ntlm_name *gss_name_t;
      typedef struct ntlm_cred *gss_cred_id_t;
      typedef struct ntlm_ctx *gss_ctx_id_t;
      typedef int gss_cred_usage_t;
      typedef OM_uint32 gss_qop_t;

      #define EXPORT __declspec(dllexport)
      #define GSS_S_COMPLETE 0
      #define GSS_S_CONTINUE_NEEDED 1
      #define GSS_S_BAD_MECH (1u << 16)
      #define GSS_S_BAD_NAME (2u << 16)
      #define GSS_S_BAD_NAMETYPE (3u << 16)
      #define GSS_S_BAD_SIG (6u << 16)
      #define GSS_S_NO_CRED (7u << 16)
      #define GSS_S_NO_CONTEXT (8u << 16)
      #define GSS_S_DEFECTIVE_TOKEN (9u << 16)
      #define GSS_S_DEFECTIVE_CREDENTIAL (10u << 16)
      #define GSS_S_CREDENTIALS_EXPIRED (11u << 16)
      #define GSS_S_CONTEXT_EXPIRED (12u << 16)
      #define GSS_S_FAILURE (13u << 16)
      #define GSS_S_UNAVAILABLE (16u << 16)
      #define GSS_C_DELEG_FLAG 1
      #define GSS_C_MUTUAL_FLAG 2
      #define GSS_C_REPLAY_FLAG 4
      #define GSS_C_SEQUENCE_FLAG 8
      #define GSS_C_CONF_FLAG 16
      #define GSS_C_INTEG_FLAG 32
      #define GSS_C_ANON_FLAG 64
      #define GSS_C_PROT_READY_FLAG 128
      #define GSS_C_TRANS_FLAG 256
      #define GSS_C_BOTH 0
      #define GSS_C_INITIATE 1
      #define GSS_C_ACCEPT 2
      #define GSS_C_INDEFINITE 0xfffffffful

      static gss_OID_desc ntlmssp_oid = { 10, "\x2b\x06\x01\x04\x01\x82\x37\x02\x02\x0a" };
      static gss_OID_desc nt_user_name = { 10, "\x2a\x86\x48\x86\xf7\x12\x01\x02\x01\x01" };
      static gss_OID_desc nt_hostbased = { 10, "\x2a\x86\x48\x86\xf7\x12\x01\x02\x01\x04" };

      struct ntlm_name { int user; char *name; };   /* user: user name; else service@host */
      struct ntlm_cred { CredHandle h; int usage; struct ntlm_name *name; };
      struct ntlm_ctx {
          CtxtHandle h;
          CredHandle cred;            /* owned when the context acquired its own credential */
          int has_ctx, own_cred, initiator, established;
          ULONG attrs;
          SecPkgContext_Sizes sizes;
          char *peer, *target;
      };

      static OM_uint32 fail(OM_uint32 *minor, SECURITY_STATUS s, OM_uint32 major)
      {
          if (minor) *minor = (OM_uint32)s;
          return major;
      }

      static int oid_eq(gss_const_OID a, gss_const_OID b)
      {
          return a && b && a->length == b->length && memcmp(a->elements, b->elements, a->length) == 0;
      }

      static char *dupn(const void *p, size_t n)
      {
          char *s = malloc(n + 1);
          if (!s) return NULL;
          memcpy(s, p, n);
          s[n] = 0;
          return s;
      }

      static wchar_t *widen(const char *s)
      {
          int n;
          wchar_t *w;
          if (!s) return NULL;
          n = MultiByteToWideChar(CP_UTF8, 0, s, -1, NULL, 0);
          w = malloc(sizeof(wchar_t) * (size_t)n);
          if (w) MultiByteToWideChar(CP_UTF8, 0, s, -1, w, n);
          return w;
      }

      static char *narrow(const wchar_t *w)
      {
          int n;
          char *s;
          if (!w) return NULL;
          n = WideCharToMultiByte(CP_UTF8, 0, w, -1, NULL, 0, NULL, NULL);
          s = malloc((size_t)n);
          if (s) WideCharToMultiByte(CP_UTF8, 0, w, -1, s, n, NULL, NULL);
          return s;
      }

      static OM_uint32 set_buffer(gss_buffer_t out, const void *p, size_t n)
      {
          if (!out) return GSS_S_COMPLETE;
          out->length = 0;
          out->value = NULL;
          if (n == 0) return GSS_S_COMPLETE;
          out->value = malloc(n);
          if (!out->value) return GSS_S_FAILURE;
          memcpy(out->value, p, n);
          out->length = n;
          return GSS_S_COMPLETE;
      }

      /* ---- names ---- */

      EXPORT OM_uint32 gssi_import_name(OM_uint32 *minor, gss_buffer_t input, gss_OID type, gss_name_t *out)
      {
          struct ntlm_name *n;
          if (!input || !out) return fail(minor, 0, GSS_S_BAD_NAME);
          if (type && !oid_eq(type, &nt_user_name) && !oid_eq(type, &nt_hostbased)) return fail(minor, 0, GSS_S_BAD_NAMETYPE);
          n = calloc(1, sizeof(*n));
          if (!n) return fail(minor, 0, GSS_S_FAILURE);
          n->user = !type || oid_eq(type, &nt_user_name);
          n->name = dupn(input->value, input->length);
          *out = n;
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      EXPORT OM_uint32 gssi_import_name_by_mech(OM_uint32 *minor, gss_const_OID mech, gss_buffer_t input, gss_OID type, gss_name_t *out)
      {
          if (mech && !oid_eq(mech, &ntlmssp_oid)) return fail(minor, 0, GSS_S_BAD_MECH);
          return gssi_import_name(minor, input, type, out);
      }

      EXPORT OM_uint32 gssi_duplicate_name(OM_uint32 *minor, const gss_name_t in, gss_name_t *out)
      {
          struct ntlm_name *n;
          if (!in || !out) return fail(minor, 0, GSS_S_BAD_NAME);
          n = calloc(1, sizeof(*n));
          if (!n) return fail(minor, 0, GSS_S_FAILURE);
          n->user = in->user;
          n->name = in->name ? dupn(in->name, strlen(in->name)) : NULL;
          *out = n;
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      EXPORT OM_uint32 gssi_display_name(OM_uint32 *minor, gss_name_t in, gss_buffer_t out, gss_OID *type)
      {
          if (!in) return fail(minor, 0, GSS_S_BAD_NAME);
          if (type) *type = in->user ? &nt_user_name : &nt_hostbased;
          return fail(minor, 0, set_buffer(out, in->name ? in->name : "", in->name ? strlen(in->name) : 0));
      }

      EXPORT OM_uint32 gssi_release_name(OM_uint32 *minor, gss_name_t *name)
      {
          if (name && *name) { free((*name)->name); free(*name); *name = NULL; }
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      /* ---- credentials ---- */

      /* "DOMAIN\user" or "user@domain" into the parts SEC_WINNT_AUTH_IDENTITY wants. */
      static void split_user(const wchar_t *full, wchar_t **user, wchar_t **domain)
      {
          const wchar_t *bs = wcschr(full, L'\\'), *at = wcschr(full, L'@');
          *user = *domain = NULL;
          if (bs) { *domain = _wcsdup(full); (*domain)[bs - full] = 0; *user = _wcsdup(bs + 1); }
          else if (at) { *user = _wcsdup(full); (*user)[at - full] = 0; *domain = _wcsdup(at + 1); }
          else *user = _wcsdup(full);
      }

      static OM_uint32 acquire(OM_uint32 *minor, gss_name_t name, gss_buffer_t password, gss_cred_usage_t usage, gss_cred_id_t *out, OM_uint32 *time_rec)
      {
          struct ntlm_cred *c;
          SEC_WINNT_AUTH_IDENTITY_W id;
          wchar_t *wname = NULL, *user = NULL, *domain = NULL, *pass = NULL;
          TimeStamp expiry;
          SECURITY_STATUS s;
          ULONG use = usage == GSS_C_INITIATE ? SECPKG_CRED_OUTBOUND : usage == GSS_C_ACCEPT ? SECPKG_CRED_INBOUND : SECPKG_CRED_BOTH;
          if (!out) return fail(minor, 0, GSS_S_FAILURE);
          c = calloc(1, sizeof(*c));
          if (!c) return fail(minor, 0, GSS_S_FAILURE);
          memset(&id, 0, sizeof(id));
          if (password && name && name->name)
          {
              wname = widen(name->name);
              split_user(wname, &user, &domain);
              pass = malloc((password->length + 1) * sizeof(wchar_t));
              if (pass)
              {
                  int n = MultiByteToWideChar(CP_UTF8, 0, password->value, (int)password->length, pass, (int)password->length);
                  pass[n < 0 ? 0 : n] = 0;
              }
              id.User = (unsigned short *)user; id.UserLength = user ? (ULONG)wcslen(user) : 0;
              id.Domain = (unsigned short *)domain; id.DomainLength = domain ? (ULONG)wcslen(domain) : 0;
              id.Password = (unsigned short *)pass; id.PasswordLength = pass ? (ULONG)wcslen(pass) : 0;
              id.Flags = SEC_WINNT_AUTH_IDENTITY_UNICODE;
          }
          s = AcquireCredentialsHandleW(NULL, L"NTLM", use, NULL, password ? &id : NULL, NULL, NULL, &c->h, &expiry);
          if (pass) { SecureZeroMemory(pass, wcslen(pass) * sizeof(wchar_t)); free(pass); }
          free(wname); free(user); free(domain);
          if (s != SEC_E_OK) { free(c); return fail(minor, s, GSS_S_NO_CRED); }
          c->usage = usage;
          if (name) gssi_duplicate_name(minor, name, &c->name);
          *out = c;
          if (time_rec) *time_rec = GSS_C_INDEFINITE;
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      static OM_uint32 mech_set(gss_OID_set *set)
      {
          if (!set) return GSS_S_COMPLETE;
          *set = malloc(sizeof(gss_OID_set_desc));
          if (!*set) return GSS_S_FAILURE;
          (*set)->count = 1;
          (*set)->elements = malloc(sizeof(gss_OID_desc));
          if (!(*set)->elements) { free(*set); *set = NULL; return GSS_S_FAILURE; }
          (*set)->elements[0].length = ntlmssp_oid.length;
          (*set)->elements[0].elements = malloc(ntlmssp_oid.length);
          if ((*set)->elements[0].elements) memcpy((*set)->elements[0].elements, ntlmssp_oid.elements, ntlmssp_oid.length);
          return GSS_S_COMPLETE;
      }

      EXPORT OM_uint32 gssi_acquire_cred(OM_uint32 *minor, const gss_name_t name, OM_uint32 time_req, const gss_OID_set mechs, gss_cred_usage_t usage,
                                         gss_cred_id_t *out, gss_OID_set *actual, OM_uint32 *time_rec)
      {
          OM_uint32 r = acquire(minor, name, NULL, usage, out, time_rec);
          (void)time_req; (void)mechs;
          if (r == GSS_S_COMPLETE) mech_set(actual);
          return r;
      }

      EXPORT OM_uint32 gssi_acquire_cred_with_password(OM_uint32 *minor, const gss_name_t name, const gss_buffer_t password, OM_uint32 time_req,
                                                       const gss_OID_set mechs, gss_cred_usage_t usage, gss_cred_id_t *out, gss_OID_set *actual, OM_uint32 *time_rec)
      {
          OM_uint32 r = acquire(minor, name, password, usage, out, time_rec);
          (void)time_req; (void)mechs;
          if (r == GSS_S_COMPLETE) mech_set(actual);
          return r;
      }

      EXPORT OM_uint32 gssi_release_cred(OM_uint32 *minor, gss_cred_id_t *cred)
      {
          if (cred && *cred)
          {
              FreeCredentialsHandle(&(*cred)->h);
              gssi_release_name(minor, &(*cred)->name);
              free(*cred);
              *cred = NULL;
          }
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      EXPORT OM_uint32 gssi_inquire_cred(OM_uint32 *minor, gss_cred_id_t cred, gss_name_t *name, OM_uint32 *lifetime, gss_cred_usage_t *usage, gss_OID_set *mechs)
      {
          if (name)
          {
              *name = NULL;
              if (cred && cred->name) gssi_duplicate_name(minor, cred->name, name);
              else
              {
                  SecPkgCredentials_NamesW names;
                  if (cred && QueryCredentialsAttributesW(&cred->h, SECPKG_CRED_ATTR_NAMES, &names) == SEC_E_OK)
                  {
                      struct ntlm_name *n = calloc(1, sizeof(*n));
                      if (n) { n->user = 1; n->name = narrow(names.sUserName); *name = n; }
                      FreeContextBuffer(names.sUserName);
                  }
              }
          }
          if (lifetime) *lifetime = GSS_C_INDEFINITE;
          if (usage) *usage = cred ? cred->usage : GSS_C_BOTH;
          return fail(minor, 0, mech_set(mechs));
      }

      /* ---- contexts ---- */

      static ULONG isc_flags(OM_uint32 req)
      {
          ULONG f = ISC_REQ_ALLOCATE_MEMORY | ISC_REQ_CONNECTION;
          if (req & GSS_C_CONF_FLAG) f |= ISC_REQ_CONFIDENTIALITY;
          if (req & (GSS_C_INTEG_FLAG | GSS_C_CONF_FLAG)) f |= ISC_REQ_INTEGRITY;
          if (req & GSS_C_REPLAY_FLAG) f |= ISC_REQ_REPLAY_DETECT;
          if (req & GSS_C_SEQUENCE_FLAG) f |= ISC_REQ_SEQUENCE_DETECT;
          if (req & GSS_C_MUTUAL_FLAG) f |= ISC_REQ_MUTUAL_AUTH;
          return f;
      }

      static OM_uint32 gss_flags(ULONG attrs, int initiator)
      {
          OM_uint32 f = GSS_C_TRANS_FLAG;
          if (initiator ? (attrs & ISC_RET_CONFIDENTIALITY) : (attrs & ASC_RET_CONFIDENTIALITY)) f |= GSS_C_CONF_FLAG;
          if (initiator ? (attrs & ISC_RET_INTEGRITY) : (attrs & ASC_RET_INTEGRITY)) f |= GSS_C_INTEG_FLAG;
          if (initiator ? (attrs & ISC_RET_REPLAY_DETECT) : (attrs & ASC_RET_REPLAY_DETECT)) f |= GSS_C_REPLAY_FLAG;
          if (initiator ? (attrs & ISC_RET_SEQUENCE_DETECT) : (attrs & ASC_RET_SEQUENCE_DETECT)) f |= GSS_C_SEQUENCE_FLAG;
          return f;
      }

      /* Channel bindings in SSPI's SEC_CHANNEL_BINDINGS layout (structure followed by the data). */
      static void *bindings(gss_channel_bindings_t cb, ULONG *size)
      {
          SEC_CHANNEL_BINDINGS *b;
          ULONG total;
          if (!cb) return NULL;
          total = sizeof(*b) + (ULONG)cb->initiator_address.length + (ULONG)cb->acceptor_address.length + (ULONG)cb->application_data.length;
          b = calloc(1, total);
          if (!b) return NULL;
          b->dwInitiatorAddrType = cb->initiator_addrtype;
          b->cbInitiatorLength = (ULONG)cb->initiator_address.length;
          b->dwInitiatorOffset = sizeof(*b);
          memcpy((char *)b + b->dwInitiatorOffset, cb->initiator_address.value, cb->initiator_address.length);
          b->dwAcceptorAddrType = cb->acceptor_addrtype;
          b->cbAcceptorLength = (ULONG)cb->acceptor_address.length;
          b->dwAcceptorOffset = b->dwInitiatorOffset + b->cbInitiatorLength;
          memcpy((char *)b + b->dwAcceptorOffset, cb->acceptor_address.value, cb->acceptor_address.length);
          b->cbApplicationDataLength = (ULONG)cb->application_data.length;
          b->dwApplicationDataOffset = b->dwAcceptorOffset + b->cbAcceptorLength;
          memcpy((char *)b + b->dwApplicationDataOffset, cb->application_data.value, cb->application_data.length);
          *size = total;
          return b;
      }

      static void finish(struct ntlm_ctx *c)
      {
          SecPkgContext_NamesW names;
          c->established = 1;
          QueryContextAttributesW(&c->h, SECPKG_ATTR_SIZES, &c->sizes);
          if (QueryContextAttributesW(&c->h, SECPKG_ATTR_NAMES, &names) == SEC_E_OK)
          {
              free(c->peer);
              c->peer = narrow(names.sUserName);
              FreeContextBuffer(names.sUserName);
          }
      }

      EXPORT OM_uint32 gssi_init_sec_context(OM_uint32 *minor, gss_cred_id_t cred, gss_ctx_id_t *context, gss_name_t target, gss_OID mech,
                                             OM_uint32 req_flags, OM_uint32 time_req, gss_channel_bindings_t cb, gss_buffer_t input,
                                             gss_OID *actual_mech, gss_buffer_t output, OM_uint32 *ret_flags, OM_uint32 *time_rec)
      {
          struct ntlm_ctx *c;
          SecBuffer in[2], out;
          SecBufferDesc indesc, outdesc;
          TimeStamp expiry;
          SECURITY_STATUS s;
          wchar_t *spn = NULL;
          void *cbdata = NULL;
          ULONG cbsize = 0, nin = 0;
          (void)time_req;
          if (output) { output->length = 0; output->value = NULL; }
          if (mech && !oid_eq(mech, &ntlmssp_oid)) return fail(minor, 0, GSS_S_BAD_MECH);
          if (!context) return fail(minor, 0, GSS_S_NO_CONTEXT);
          c = *context;
          if (!c)
          {
              c = calloc(1, sizeof(*c));
              if (!c) return fail(minor, 0, GSS_S_FAILURE);
              c->initiator = 1;
              if (cred) c->cred = cred->h;
              else
              {
                  s = AcquireCredentialsHandleW(NULL, L"NTLM", SECPKG_CRED_OUTBOUND, NULL, NULL, NULL, NULL, &c->cred, &expiry);
                  if (s != SEC_E_OK) { free(c); return fail(minor, s, GSS_S_NO_CRED); }
                  c->own_cred = 1;
              }
              if (target && target->name)
              {
                  /* GSS host-based names are service@host; SPNs are service/host. */
                  char *t = dupn(target->name, strlen(target->name)), *at = strchr(t, '@');
                  if (!target->user && at) *at = '/';
                  c->target = t;
              }
              *context = c;
          }
          if (c->target) spn = widen(c->target);
          if (input && input->length)
          {
              in[nin].BufferType = SECBUFFER_TOKEN; in[nin].cbBuffer = (ULONG)input->length; in[nin].pvBuffer = input->value; nin++;
          }
          if (cb && (cbdata = bindings(cb, &cbsize)))
          {
              in[nin].BufferType = SECBUFFER_CHANNEL_BINDINGS; in[nin].cbBuffer = cbsize; in[nin].pvBuffer = cbdata; nin++;
          }
          indesc.ulVersion = SECBUFFER_VERSION; indesc.cBuffers = nin; indesc.pBuffers = in;
          out.BufferType = SECBUFFER_TOKEN; out.cbBuffer = 0; out.pvBuffer = NULL;
          outdesc.ulVersion = SECBUFFER_VERSION; outdesc.cBuffers = 1; outdesc.pBuffers = &out;
          s = InitializeSecurityContextW(&c->cred, c->has_ctx ? &c->h : NULL, spn, isc_flags(req_flags), 0, SECURITY_NATIVE_DREP,
                                         nin ? &indesc : NULL, 0, &c->h, &outdesc, &c->attrs, &expiry);
          free(spn);
          free(cbdata);
          if (s != SEC_E_OK && s != SEC_I_CONTINUE_NEEDED && s != SEC_I_COMPLETE_NEEDED && s != SEC_I_COMPLETE_AND_CONTINUE)
              return fail(minor, s, s == SEC_E_INVALID_TOKEN ? GSS_S_DEFECTIVE_TOKEN : GSS_S_FAILURE);
          c->has_ctx = 1;
          if (s == SEC_I_COMPLETE_NEEDED || s == SEC_I_COMPLETE_AND_CONTINUE) CompleteAuthToken(&c->h, &outdesc);
          if (set_buffer(output, out.pvBuffer, out.cbBuffer) != GSS_S_COMPLETE) { FreeContextBuffer(out.pvBuffer); return fail(minor, 0, GSS_S_FAILURE); }
          if (out.pvBuffer) FreeContextBuffer(out.pvBuffer);
          if (actual_mech) *actual_mech = &ntlmssp_oid;
          if (time_rec) *time_rec = GSS_C_INDEFINITE;
          if (s == SEC_E_OK || s == SEC_I_COMPLETE_NEEDED) finish(c);
          if (ret_flags) *ret_flags = gss_flags(c->attrs, 1);
          return fail(minor, 0, c->established ? GSS_S_COMPLETE : GSS_S_CONTINUE_NEEDED);
      }

      EXPORT OM_uint32 gssi_accept_sec_context(OM_uint32 *minor, gss_ctx_id_t *context, gss_cred_id_t cred, gss_buffer_t input, gss_channel_bindings_t cb,
                                               gss_name_t *src_name, gss_OID *mech, gss_buffer_t output, OM_uint32 *ret_flags, OM_uint32 *time_rec,
                                               gss_cred_id_t *delegated)
      {
          struct ntlm_ctx *c;
          SecBuffer in[2], out;
          SecBufferDesc indesc, outdesc;
          TimeStamp expiry;
          SECURITY_STATUS s;
          void *cbdata = NULL;
          ULONG cbsize = 0, nin = 0;
          if (output) { output->length = 0; output->value = NULL; }
          if (delegated) *delegated = NULL;
          if (!context || !input || !input->length) return fail(minor, 0, GSS_S_DEFECTIVE_TOKEN);
          c = *context;
          if (!c)
          {
              c = calloc(1, sizeof(*c));
              if (!c) return fail(minor, 0, GSS_S_FAILURE);
              if (cred) c->cred = cred->h;
              else
              {
                  s = AcquireCredentialsHandleW(NULL, L"NTLM", SECPKG_CRED_INBOUND, NULL, NULL, NULL, NULL, &c->cred, &expiry);
                  if (s != SEC_E_OK) { free(c); return fail(minor, s, GSS_S_NO_CRED); }
                  c->own_cred = 1;
              }
              *context = c;
          }
          in[nin].BufferType = SECBUFFER_TOKEN; in[nin].cbBuffer = (ULONG)input->length; in[nin].pvBuffer = input->value; nin++;
          if (cb && (cbdata = bindings(cb, &cbsize)))
          {
              in[nin].BufferType = SECBUFFER_CHANNEL_BINDINGS; in[nin].cbBuffer = cbsize; in[nin].pvBuffer = cbdata; nin++;
          }
          indesc.ulVersion = SECBUFFER_VERSION; indesc.cBuffers = nin; indesc.pBuffers = in;
          out.BufferType = SECBUFFER_TOKEN; out.cbBuffer = 0; out.pvBuffer = NULL;
          outdesc.ulVersion = SECBUFFER_VERSION; outdesc.cBuffers = 1; outdesc.pBuffers = &out;
          s = AcceptSecurityContext(&c->cred, c->has_ctx ? &c->h : NULL, &indesc,
                                    ASC_REQ_ALLOCATE_MEMORY | ASC_REQ_CONNECTION | ASC_REQ_CONFIDENTIALITY | ASC_REQ_INTEGRITY | ASC_REQ_REPLAY_DETECT | ASC_REQ_SEQUENCE_DETECT,
                                    SECURITY_NATIVE_DREP, &c->h, &outdesc, &c->attrs, &expiry);
          free(cbdata);
          if (s != SEC_E_OK && s != SEC_I_CONTINUE_NEEDED && s != SEC_I_COMPLETE_NEEDED && s != SEC_I_COMPLETE_AND_CONTINUE)
              return fail(minor, s, s == SEC_E_LOGON_DENIED ? GSS_S_DEFECTIVE_CREDENTIAL : s == SEC_E_INVALID_TOKEN ? GSS_S_DEFECTIVE_TOKEN : GSS_S_FAILURE);
          c->has_ctx = 1;
          if (s == SEC_I_COMPLETE_NEEDED || s == SEC_I_COMPLETE_AND_CONTINUE) CompleteAuthToken(&c->h, &outdesc);
          if (set_buffer(output, out.pvBuffer, out.cbBuffer) != GSS_S_COMPLETE) { FreeContextBuffer(out.pvBuffer); return fail(minor, 0, GSS_S_FAILURE); }
          if (out.pvBuffer) FreeContextBuffer(out.pvBuffer);
          if (mech) *mech = &ntlmssp_oid;
          if (time_rec) *time_rec = GSS_C_INDEFINITE;
          if (s == SEC_E_OK || s == SEC_I_COMPLETE_NEEDED)
          {
              finish(c);
              if (src_name)
              {
                  struct ntlm_name *n = calloc(1, sizeof(*n));
                  if (n) { n->user = 1; n->name = c->peer ? dupn(c->peer, strlen(c->peer)) : NULL; }
                  *src_name = n;
              }
          }
          if (ret_flags) *ret_flags = gss_flags(c->attrs, 0);
          return fail(minor, 0, c->established ? GSS_S_COMPLETE : GSS_S_CONTINUE_NEEDED);
      }

      EXPORT OM_uint32 gssi_delete_sec_context(OM_uint32 *minor, gss_ctx_id_t *context, gss_buffer_t output)
      {
          if (output) { output->length = 0; output->value = NULL; }
          if (context && *context)
          {
              struct ntlm_ctx *c = *context;
              if (c->has_ctx) DeleteSecurityContext(&c->h);
              if (c->own_cred) FreeCredentialsHandle(&c->cred);
              free(c->peer);
              free(c->target);
              free(c);
              *context = NULL;
          }
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      EXPORT OM_uint32 gssi_context_time(OM_uint32 *minor, gss_ctx_id_t c, OM_uint32 *time_rec)
      {
          if (!c) return fail(minor, 0, GSS_S_NO_CONTEXT);
          if (time_rec) *time_rec = GSS_C_INDEFINITE;
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      EXPORT OM_uint32 gssi_inquire_context(OM_uint32 *minor, gss_ctx_id_t c, gss_name_t *src, gss_name_t *targ, OM_uint32 *lifetime, gss_OID *mech,
                                            OM_uint32 *flags, int *locally_initiated, int *open)
      {
          if (!c) return fail(minor, 0, GSS_S_NO_CONTEXT);
          if (src)
          {
              *src = NULL;
              if (c->peer && !c->initiator) { struct ntlm_name *n = calloc(1, sizeof(*n)); if (n) { n->user = 1; n->name = dupn(c->peer, strlen(c->peer)); } *src = n; }
          }
          if (targ)
          {
              *targ = NULL;
              if (c->target) { struct ntlm_name *n = calloc(1, sizeof(*n)); if (n) n->name = dupn(c->target, strlen(c->target)); *targ = n; }
          }
          if (lifetime) *lifetime = GSS_C_INDEFINITE;
          if (mech) *mech = &ntlmssp_oid;
          if (flags) *flags = gss_flags(c->attrs, c->initiator);
          if (locally_initiated) *locally_initiated = c->initiator;
          if (open) *open = c->established;
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      /* ---- per-message protection: SSPI MakeSignature/EncryptMessage, signature first ---- */

      EXPORT OM_uint32 gssi_get_mic(OM_uint32 *minor, gss_ctx_id_t c, gss_qop_t qop, gss_buffer_t message, gss_buffer_t token)
      {
          SecBuffer b[2];
          SecBufferDesc d;
          SECURITY_STATUS s;
          void *sig;
          (void)qop;
          if (!c || !c->established) return fail(minor, 0, GSS_S_NO_CONTEXT);
          sig = malloc(c->sizes.cbMaxSignature);
          if (!sig) return fail(minor, 0, GSS_S_FAILURE);
          b[0].BufferType = SECBUFFER_DATA; b[0].cbBuffer = (ULONG)message->length; b[0].pvBuffer = message->value;
          b[1].BufferType = SECBUFFER_TOKEN; b[1].cbBuffer = c->sizes.cbMaxSignature; b[1].pvBuffer = sig;
          d.ulVersion = SECBUFFER_VERSION; d.cBuffers = 2; d.pBuffers = b;
          s = MakeSignature(&c->h, 0, &d, 0);
          if (s != SEC_E_OK) { free(sig); return fail(minor, s, GSS_S_FAILURE); }
          token->length = b[1].cbBuffer;
          token->value = sig;
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      EXPORT OM_uint32 gssi_verify_mic(OM_uint32 *minor, gss_ctx_id_t c, gss_buffer_t message, gss_buffer_t token, gss_qop_t *qop)
      {
          SecBuffer b[2];
          SecBufferDesc d;
          SECURITY_STATUS s;
          ULONG q = 0;
          if (!c || !c->established) return fail(minor, 0, GSS_S_NO_CONTEXT);
          b[0].BufferType = SECBUFFER_DATA; b[0].cbBuffer = (ULONG)message->length; b[0].pvBuffer = message->value;
          b[1].BufferType = SECBUFFER_TOKEN; b[1].cbBuffer = (ULONG)token->length; b[1].pvBuffer = token->value;
          d.ulVersion = SECBUFFER_VERSION; d.cBuffers = 2; d.pBuffers = b;
          s = VerifySignature(&c->h, &d, 0, &q);
          if (qop) *qop = 0;
          return s == SEC_E_OK ? fail(minor, 0, GSS_S_COMPLETE) : fail(minor, s, GSS_S_BAD_SIG);
      }

      EXPORT OM_uint32 gssi_wrap(OM_uint32 *minor, gss_ctx_id_t c, int conf_req, gss_qop_t qop, gss_buffer_t input, int *conf_state, gss_buffer_t output)
      {
          SecBuffer b[2];
          SecBufferDesc d;
          SECURITY_STATUS s;
          unsigned char *buf;
          ULONG trailer;
          (void)qop;
          if (!c || !c->established) return fail(minor, 0, GSS_S_NO_CONTEXT);
          if (!conf_req)
          {
              /* Integrity only: signature followed by the message in the clear. */
              gss_buffer_desc mic;
              OM_uint32 r = gssi_get_mic(minor, c, 0, input, &mic);
              if (r != GSS_S_COMPLETE) return r;
              output->length = mic.length + input->length;
              output->value = malloc(output->length);
              if (!output->value) { free(mic.value); return fail(minor, 0, GSS_S_FAILURE); }
              memcpy(output->value, mic.value, mic.length);
              memcpy((char *)output->value + mic.length, input->value, input->length);
              free(mic.value);
              if (conf_state) *conf_state = 0;
              return fail(minor, 0, GSS_S_COMPLETE);
          }
          trailer = c->sizes.cbSecurityTrailer;
          buf = malloc(trailer + input->length);
          if (!buf) return fail(minor, 0, GSS_S_FAILURE);
          memcpy(buf + trailer, input->value, input->length);
          b[0].BufferType = SECBUFFER_TOKEN; b[0].cbBuffer = trailer; b[0].pvBuffer = buf;
          b[1].BufferType = SECBUFFER_DATA; b[1].cbBuffer = (ULONG)input->length; b[1].pvBuffer = buf + trailer;
          d.ulVersion = SECBUFFER_VERSION; d.cBuffers = 2; d.pBuffers = b;
          s = EncryptMessage(&c->h, 0, &d, 0);
          if (s != SEC_E_OK) { free(buf); return fail(minor, s, GSS_S_FAILURE); }
          if (b[0].cbBuffer < trailer) memmove(buf + b[0].cbBuffer, buf + trailer, input->length);
          output->value = buf;
          output->length = b[0].cbBuffer + input->length;
          if (conf_state) *conf_state = 1;
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      EXPORT OM_uint32 gssi_unwrap(OM_uint32 *minor, gss_ctx_id_t c, gss_buffer_t input, gss_buffer_t output, int *conf_state, gss_qop_t *qop)
      {
          SecBuffer b[2];
          SecBufferDesc d;
          SECURITY_STATUS s;
          ULONG q = 0, sig;
          unsigned char *copy;
          if (!c || !c->established) return fail(minor, 0, GSS_S_NO_CONTEXT);
          sig = c->sizes.cbMaxSignature;
          if (input->length < sig) return fail(minor, 0, GSS_S_DEFECTIVE_TOKEN);
          copy = malloc(input->length);
          if (!copy) return fail(minor, 0, GSS_S_FAILURE);
          memcpy(copy, input->value, input->length);
          b[0].BufferType = SECBUFFER_TOKEN; b[0].cbBuffer = sig; b[0].pvBuffer = copy;
          b[1].BufferType = SECBUFFER_DATA; b[1].cbBuffer = (ULONG)(input->length - sig); b[1].pvBuffer = copy + sig;
          d.ulVersion = SECBUFFER_VERSION; d.cBuffers = 2; d.pBuffers = b;
          s = DecryptMessage(&c->h, &d, 0, &q);
          if (s != SEC_E_OK)
          {
              /* Not sealed? Then it may be an integrity-only token: signature + clear text. */
              gss_buffer_desc msg = { input->length - sig, (char *)input->value + sig }, tok = { sig, input->value };
              free(copy);
              if (gssi_verify_mic(minor, c, &msg, &tok, qop) != GSS_S_COMPLETE) return fail(minor, s, GSS_S_BAD_SIG);
              if (conf_state) *conf_state = 0;
              return fail(minor, 0, set_buffer(output, msg.value, msg.length));
          }
          memmove(copy, copy + sig, input->length - sig);
          output->value = copy;
          output->length = input->length - sig;
          if (conf_state) *conf_state = 1;
          if (qop) *qop = 0;
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      EXPORT OM_uint32 gssi_wrap_size_limit(OM_uint32 *minor, gss_ctx_id_t c, int conf_req, gss_qop_t qop, OM_uint32 req_output_size, OM_uint32 *max_input_size)
      {
          ULONG over;
          (void)qop;
          if (!c || !c->established) return fail(minor, 0, GSS_S_NO_CONTEXT);
          over = conf_req ? c->sizes.cbSecurityTrailer : c->sizes.cbMaxSignature;
          *max_input_size = req_output_size > over ? req_output_size - over : 0;
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      /* ---- mechanism information ---- */

      EXPORT OM_uint32 gssi_indicate_mechs(OM_uint32 *minor, gss_OID_set *set)
      {
          return fail(minor, 0, mech_set(set));
      }

      EXPORT OM_uint32 gssi_inquire_names_for_mech(OM_uint32 *minor, gss_OID mech, gss_OID_set *types)
      {
          gss_OID_desc *e;
          (void)mech;
          *types = malloc(sizeof(gss_OID_set_desc));
          if (!*types) return fail(minor, 0, GSS_S_FAILURE);
          e = malloc(2 * sizeof(gss_OID_desc));
          if (!e) { free(*types); return fail(minor, 0, GSS_S_FAILURE); }
          e[0].length = nt_user_name.length; e[0].elements = dupn(nt_user_name.elements, nt_user_name.length);
          e[1].length = nt_hostbased.length; e[1].elements = dupn(nt_hostbased.elements, nt_hostbased.length);
          (*types)->count = 2;
          (*types)->elements = e;
          return fail(minor, 0, GSS_S_COMPLETE);
      }

      EXPORT OM_uint32 gssi_display_status(OM_uint32 *minor, OM_uint32 status, int type, gss_OID mech, OM_uint32 *ctx, gss_buffer_t out)
      {
          wchar_t *msg = NULL;
          char *text;
          OM_uint32 r;
          (void)mech; (void)type;
          if (ctx) *ctx = 0;
          if (!FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, status, 0, (LPWSTR)&msg, 0, NULL))
              return fail(minor, 0, set_buffer(out, "Unknown NTLMSSP error", 21));
          text = narrow(msg);
          LocalFree(msg);
          r = set_buffer(out, text, text ? strcspn(text, "\r\n") : 0);
          free(text);
          return fail(minor, 0, r);
      }
    C
    (lib/"gssntlmssp").mkpath
    system ENV.cc, "-O2", "-shared", "-o", lib/"gssntlmssp/gssntlmssp.dll", "win/gssntlmssp.c", "-lsecur32"
    (include/"gssapi").install "src/gssapi_ntlmssp.h" if File.exist?("src/gssapi_ntlmssp.h")
    dll = (lib/"gssntlmssp/gssntlmssp.dll").to_s.tr("\\", "/")
    (etc/"gss/mech.d/ntlmssp.conf").write <<~EOS
      # NTLMSSP mechanism for GSSAPI (gss-ntlmssp on Windows SSPI)
      gssntlmssp_v1    1.3.6.1.4.1.311.2.2.10    #{lib}/gssntlmssp/gssntlmssp.dll
    EOS
    (pkgshare/"gssntlmssp.reg").write <<~EOS
      Windows Registry Editor Version 5.00

      [HKEY_LOCAL_MACHINE\\SOFTWARE\\gss\\mech\\gssntlmssp_v1]
      "OID"="1.3.6.1.4.1.311.2.2.10"
      "Shared Library"="#{dll}"
    EOS
    doc.install "README.md" if File.exist?("README.md")
  end

  def caveats
    <<~EOS
      To use the mechanism with MIT Kerberos for Windows' GSSAPI, register it once (as admin):
        reg import #{opt_pkgshare}/gssntlmssp.reg
    EOS
  end

  test do
    # Verify the mechanism config is installed with the correct library path
    conf = (etc/"gss/mech.d/ntlmssp.conf").read
    assert_match "gssntlmssp", conf
    assert_match lib.to_s, conf

    # Load the plugin like a mechglue and authenticate the current user to itself through NTLM.
    (testpath/"test.c").write <<~'C'
      #include <windows.h>
      #include <stdint.h>
      #include <stdio.h>
      #include <string.h>

      /* Loads the mechanism like a mechglue would and runs an NTLM handshake with itself (the current
       * user, through Windows' NTLM package), then a wrap/unwrap round trip. */
      typedef uint32_t OM_uint32;
      typedef struct { size_t length; void *value; } buf_t;
      typedef OM_uint32 (*init_t)(OM_uint32 *, void *, void **, void *, void *, OM_uint32, OM_uint32, void *, buf_t *, void **, buf_t *, OM_uint32 *, OM_uint32 *);
      typedef OM_uint32 (*accept_t)(OM_uint32 *, void **, void *, buf_t *, void *, void **, void **, buf_t *, OM_uint32 *, OM_uint32 *, void **);
      typedef OM_uint32 (*wrap_t)(OM_uint32 *, void *, int, OM_uint32, buf_t *, int *, buf_t *);
      typedef OM_uint32 (*unwrap_t)(OM_uint32 *, void *, buf_t *, buf_t *, int *, OM_uint32 *);
      typedef OM_uint32 (*display_t)(OM_uint32 *, void *, buf_t *, void **);

      int main(int argc, char **argv)
      {
          HMODULE m = LoadLibraryA(argc > 1 ? argv[1] : "gssntlmssp.dll");
          init_t init; accept_t accept; wrap_t wrap; unwrap_t unwrap; display_t display;
          void *ictx = NULL, *actx = NULL, *src = NULL;
          buf_t itok = {0, NULL}, atok = {0, NULL}, msg = {5, "hello"}, sealed, plain, name;
          OM_uint32 minor, ri = 1, ra = 1;
          int conf, rounds = 0;
          if (!m) { printf("cannot load the mechanism\n"); return 1; }
          init = (init_t)GetProcAddress(m, "gssi_init_sec_context");
          accept = (accept_t)GetProcAddress(m, "gssi_accept_sec_context");
          wrap = (wrap_t)GetProcAddress(m, "gssi_wrap");
          unwrap = (unwrap_t)GetProcAddress(m, "gssi_unwrap");
          display = (display_t)GetProcAddress(m, "gssi_display_name");
          if (!init || !accept || !wrap || !unwrap || !display) { printf("missing gssi_ entry points\n"); return 1; }
          while ((ri != 0 || ra != 0) && rounds++ < 5)
          {
              ri = init(&minor, NULL, &ictx, NULL, NULL, 16 | 32, 0, NULL, &atok, NULL, &itok, NULL, NULL);
              if (ri > 1) { printf("init failed: %u (0x%x)\n", ri, minor); return 1; }
              if (rounds == 1) printf("negotiate token starts with %.7s\n", (char *)itok.value);
              if (itok.length == 0) break;
              ra = accept(&minor, &actx, NULL, &itok, NULL, &src, NULL, &atok, NULL, NULL, NULL);
              if (ra > 1) { printf("accept failed: %u (0x%x)\n", ra, minor); return 1; }
          }
          if (display(&minor, src, &name, NULL) == 0) printf("authenticated as %.*s\n", (int)name.length, (char *)name.value);
          if (wrap(&minor, ictx, 1, 0, &msg, &conf, &sealed) || unwrap(&minor, actx, &sealed, &plain, &conf, NULL)) { printf("wrap failed 0x%x\n", minor); return 1; }
          printf("unwrapped: %.*s (sealed %d)\n", (int)plain.length, (char *)plain.value, conf);
          return 0;
      }
    C
    system ENV.cc, "test.c", "-o", "test.exe"
    output = shell_output("./test.exe #{lib}/gssntlmssp/gssntlmssp.dll")
    assert_match "negotiate token starts with NTLMSSP", output
    assert_match "unwrapped: hello", output
  end
end
