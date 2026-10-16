class Libnsl < Formula
  desc "Public client interface for NIS(YP)"
  homepage "https://github.com/thkukuk/libnsl"
  url "https://github.com/thkukuk/libnsl/releases/download/v2.0.1/libnsl-2.0.1.tar.xz"
  sha256 "5c9e470b232a7acd3433491ac5221b4832f0c71318618dc6aa04dd05ffcd8fd9"
  license "LGPL-2.1-or-later"

  # NIS distributed a site's passwd, group, hosts and services tables; on Windows those live in
  # the account database (local SAM accounts and groups) and the drivers\etc files. This builds
  # libnsl's NIS client API (<rpcsvc/ypclnt.h>: yp_get_default_domain, yp_match, yp_first/yp_next,
  # yp_all, yp_master, yp_order, yp_maplist) on them, with the NIS domain being the Windows domain
  # or workgroup, as libnsl-3.dll; plus yp-tools' ypcat, ypmatch, ypwhich and ypdomainname.
  # No NIS server is involved.

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    (buildpath/"windows/rpcsvc/ypclnt.h").write <<~'C'
      /* NIS (YP) client interface for Windows. The NIS domain is the computer's Windows domain or
       * workgroup, and the maps are served from this computer's own databases: passwd.* and group.*
       * from its local accounts and groups, hosts/services/protocols/networks from the files in
       * %SystemRoot%\System32\drivers\etc. No NIS server is contacted. */
      #ifndef __RPCSVC_YPCLNT_H__
      #define __RPCSVC_YPCLNT_H__

      #if defined(_WIN32) && !defined(NSL_STATIC)
      #ifdef NSL_BUILD
      #define NSL_API __declspec(dllexport)
      #else
      #define NSL_API __declspec(dllimport)
      #endif
      #else
      #define NSL_API
      #endif

      #define YPERR_SUCCESS 0  /* There is no error */
      #define YPERR_BADARGS 1  /* Args to function are bad */
      #define YPERR_RPC 2      /* RPC failure */
      #define YPERR_DOMAIN 3   /* Can't bind to a server with this domain */
      #define YPERR_MAP 4      /* No such map in server's domain */
      #define YPERR_KEY 5      /* No such key in map */
      #define YPERR_YPERR 6    /* Internal yp server or client error */
      #define YPERR_RESRC 7    /* Local resource allocation failure */
      #define YPERR_NOMORE 8   /* No more records in map database */
      #define YPERR_PMAP 9     /* Can't communicate with portmapper */
      #define YPERR_YPBIND 10  /* Can't communicate with ypbind */
      #define YPERR_YPSERV 11  /* Can't communicate with ypserv */
      #define YPERR_NODOM 12   /* Local domain name not set */
      #define YPERR_BADDB 13   /* yp data base is bad */
      #define YPERR_VERS 14    /* YP version mismatch */
      #define YPERR_ACCESS 15  /* Access violation */
      #define YPERR_BUSY 16    /* Database is busy */

      #define YPOP_CHANGE 1
      #define YPOP_INSERT 2
      #define YPOP_DELETE 3
      #define YPOP_STORE 4

      #ifdef __cplusplus
      extern "C" {
      #endif

      struct ypmaplist;

      struct ypall_callback {
          /* Return non-zero to stop; called once more with a status other than YP_TRUE at the end. */
          int (*foreach)(int __status, char *__key, int __keylen, char *__val, int __vallen, char *__data);
          char *data;
      };

      NSL_API int yp_bind(const char *);
      NSL_API void yp_unbind(const char *);
      NSL_API int yp_get_default_domain(char **);
      NSL_API int yp_match(const char *, const char *, const char *, const int, char **, int *);
      NSL_API int yp_first(const char *, const char *, char **, int *, char **, int *);
      NSL_API int yp_next(const char *, const char *, const char *, const int, char **, int *, char **, int *);
      NSL_API int yp_master(const char *, const char *, char **);
      NSL_API int yp_order(const char *, const char *, unsigned int *);
      NSL_API int yp_all(const char *, const char *, const struct ypall_callback *);
      NSL_API const char *yperr_string(const int);
      NSL_API const char *ypbinderr_string(const int);
      NSL_API int ypprot_err(const int);
      NSL_API int yp_update(char *, char *, unsigned, char *, int, char *, int);
      NSL_API int yp_maplist(const char *, struct ypmaplist **);
      NSL_API int __yp_check(char **);

      #ifdef __cplusplus
      }
      #endif

      #endif /* __RPCSVC_YPCLNT_H__ */
    C
    (buildpath/"windows/rpcsvc/yp_prot.h").write <<~'C'
      /* NIS protocol definitions used by clients (status codes, map lists, bind errors). */
      #ifndef __RPCSVC_YP_PROT_H__
      #define __RPCSVC_YP_PROT_H__

      #define YPPROG 100004
      #define YPVERS 2
      #define YPBINDPROG 100007
      #define YPBINDVERS 2

      #define YPMAXRECORD 1024
      #define YPMAXDOMAIN 64
      #define YPMAXMAP 64
      #define YPMAXPEER 64

      /* Map server status codes. */
      #define YP_TRUE 1
      #define YP_NOMORE 2
      #define YP_FALSE 0
      #define YP_NOMAP -1
      #define YP_NODOM -2
      #define YP_NOKEY -3
      #define YP_BADOP -4
      #define YP_BADDB -5
      #define YP_YPERR -6
      #define YP_BADARGS -7
      #define YP_VERS -8

      /* ypbind error codes. */
      #define YPBIND_ERR_ERR 1
      #define YPBIND_ERR_NOSERV 2
      #define YPBIND_ERR_RESC 3

      struct ypmaplist {
          char *map;
          struct ypmaplist *next;
      };

      #endif /* __RPCSVC_YP_PROT_H__ */
    C
    (buildpath/"windows/nsl.c").write <<~'C'
      /* libnsl's NIS client API on Windows. Every map is built on demand from this computer's account
       * database (NetUserEnum/NetLocalGroupEnum) or its drivers\etc files, in the text formats NIS
       * serves (passwd lines "name:*:uid:gid:gecos:home:shell", group lines "name:*:gid:members", the
       * etc files' own lines). uid/gid are the accounts' relative IDs, the last part of their SIDs. */
      #define NSL_BUILD
      #include "rpcsvc/ypclnt.h"
      #include "rpcsvc/yp_prot.h"

      #include <ctype.h>
      #include <stdio.h>
      #include <stdlib.h>
      #include <string.h>
      #include <time.h>
      #include <windows.h>
      #include <lm.h>
      #include <sddl.h>

      struct entry { char *key, *val; };
      struct map { struct entry *items; int count, cap; };

      static void map_add(struct map *m, const char *key, const char *val)
      {
          if (m->count == m->cap) {
              int cap = m->cap ? m->cap * 2 : 64;
              struct entry *grown = (struct entry *)realloc(m->items, (size_t)cap * sizeof *grown);
              if (!grown) return;
              m->items = grown;
              m->cap = cap;
          }
          m->items[m->count].key = _strdup(key);
          m->items[m->count].val = _strdup(val);
          m->count++;
      }

      static void map_free(struct map *m)
      {
          for (int i = 0; i < m->count; i++) { free(m->items[i].key); free(m->items[i].val); }
          free(m->items);
          memset(m, 0, sizeof *m);
      }

      static int compare_entry(const void *a, const void *b)
      {
          return strcmp(((const struct entry *)a)->key, ((const struct entry *)b)->key);
      }

      static void utf8(const wchar_t *w, char *out, int size)
      {
          if (!w) { out[0] = 0; return; }
          if (!WideCharToMultiByte(CP_UTF8, 0, w, -1, out, size, NULL, NULL)) out[0] = 0;
      }

      /* The relative ID (last sub-authority) of an account's SID, and the SID's string form. */
      static unsigned long account_rid(const wchar_t *name, wchar_t *sid_string, int sid_size)
      {
          BYTE sid[SECURITY_MAX_SID_SIZE];
          DWORD sid_len = sizeof sid, dom_len = 256;
          wchar_t dom[256];
          SID_NAME_USE use;
          LPWSTR text = NULL;
          if (sid_string) sid_string[0] = 0;
          if (!LookupAccountNameW(NULL, name, sid, &sid_len, dom, &dom_len, &use)) return (unsigned long)-1;
          if (sid_string && ConvertSidToStringSidW(sid, &text)) {
              lstrcpynW(sid_string, text, sid_size);
              LocalFree(text);
          }
          return *GetSidSubAuthority(sid, *GetSidSubAuthorityCount(sid) - 1);
      }

      static void profile_dir(const wchar_t *sid, char *out, int size)
      {
          wchar_t key[512], path[MAX_PATH];
          DWORD bytes = sizeof path;
          out[0] = 0;
          if (!sid[0]) return;
          _snwprintf(key, 512, L"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ProfileList\\%ls", sid);
          key[511] = 0;
          if (RegGetValueW(HKEY_LOCAL_MACHINE, key, L"ProfileImagePath", RRF_RT_REG_SZ | RRF_RT_REG_EXPAND_SZ, NULL, path, &bytes) == ERROR_SUCCESS)
              utf8(path, out, size);
      }

      /* NIS text fields can't hold ':' or newlines. */
      static void clean_field(char *s)
      {
          for (; *s; s++) if (*s == ':' || *s == '\n' || *s == '\r') *s = ' ';
      }

      static void build_passwd(struct map *m, int by_uid)
      {
          USER_INFO_20 *users = NULL;
          DWORD read = 0, total = 0, resume = 0;
          NET_API_STATUS st;
          char shell[MAX_PATH];
          wchar_t comspec[MAX_PATH];
          if (!GetEnvironmentVariableW(L"ComSpec", comspec, MAX_PATH)) lstrcpyW(comspec, L"C:\\Windows\\System32\\cmd.exe");
          utf8(comspec, shell, sizeof shell);
          do {
              st = NetUserEnum(NULL, 20, FILTER_NORMAL_ACCOUNT, (LPBYTE *)&users, MAX_PREFERRED_LENGTH, &read, &total, &resume);
              if (st != NERR_Success && st != ERROR_MORE_DATA) break;
              for (DWORD i = 0; i < read; i++) {
                  char name[256], gecos[512], home[MAX_PATH * 3], line[2048], key[32];
                  wchar_t sid[256];
                  unsigned long uid = account_rid(users[i].usri20_name, sid, 256);
                  utf8(users[i].usri20_name, name, sizeof name);
                  utf8(users[i].usri20_full_name && users[i].usri20_full_name[0] ? users[i].usri20_full_name : users[i].usri20_comment,
                       gecos, sizeof gecos);
                  clean_field(gecos);
                  profile_dir(sid, home, sizeof home);
                  /* Primary group: "None" (513), the group local accounts are given on a workstation. */
                  snprintf(line, sizeof line, "%s:*:%lu:513:%s:%s:%s", name, uid, gecos, home, shell);
                  snprintf(key, sizeof key, "%lu", uid);
                  map_add(m, by_uid ? key : name, line);
              }
              NetApiBufferFree(users);
              users = NULL;
          } while (st == ERROR_MORE_DATA);
      }

      static void build_group(struct map *m, int by_gid)
      {
          LOCALGROUP_INFO_0 *groups = NULL;
          DWORD read = 0, total = 0;
          DWORD_PTR resume = 0;
          NET_API_STATUS st;
          do {
              st = NetLocalGroupEnum(NULL, 0, (LPBYTE *)&groups, MAX_PREFERRED_LENGTH, &read, &total, &resume);
              if (st != NERR_Success && st != ERROR_MORE_DATA) break;
              for (DWORD i = 0; i < read; i++) {
                  char name[256], members[4096] = "", line[4608], key[32];
                  LOCALGROUP_MEMBERS_INFO_3 *mem = NULL;
                  DWORD mread = 0, mtotal = 0;
                  unsigned long gid = account_rid(groups[i].lgrpi0_name, NULL, 0);
                  utf8(groups[i].lgrpi0_name, name, sizeof name);
                  clean_field(name);
                  if (NetLocalGroupGetMembers(NULL, groups[i].lgrpi0_name, 3, (LPBYTE *)&mem, MAX_PREFERRED_LENGTH, &mread, &mtotal, NULL) == NERR_Success) {
                      for (DWORD j = 0; j < mread; j++) {
                          char who[512];
                          const wchar_t *slash = wcschr(mem[j].lgrmi3_domainandname, L'\\');
                          utf8(slash ? slash + 1 : mem[j].lgrmi3_domainandname, who, sizeof who);
                          for (char *c = who; *c; c++) if (*c == ',' || *c == ':') *c = '_';
                          if (strlen(members) + strlen(who) + 2 < sizeof members) {
                              if (members[0]) strcat(members, ",");
                              strcat(members, who);
                          }
                      }
                      NetApiBufferFree(mem);
                  }
                  snprintf(line, sizeof line, "%s:*:%lu:%s", name, gid, members);
                  snprintf(key, sizeof key, "%lu", gid);
                  map_add(m, by_gid ? key : name, line);
              }
              NetApiBufferFree(groups);
              groups = NULL;
          } while (st == ERROR_MORE_DATA);
      }

      /* Lines of a drivers\etc file, comments stripped, split on whitespace. */
      static void build_etc(struct map *m, const char *file, int key_field, int all_names, const char *suffix_from_field2)
      {
          char path[MAX_PATH], line[1024];
          FILE *f;
          if (!GetEnvironmentVariableA("SystemRoot", path, MAX_PATH)) strcpy(path, "C:\\Windows");
          strncat(path, "\\System32\\drivers\\etc\\", sizeof path - strlen(path) - 1);
          strncat(path, file, sizeof path - strlen(path) - 1);
          if (!(f = fopen(path, "r"))) return;
          while (fgets(line, sizeof line, f)) {
              char *fields[16], copy[1024], value[1024];
              int n = 0;
              char *hash = strchr(line, '#');
              if (hash) *hash = 0;
              line[strcspn(line, "\r\n")] = 0;
              strcpy(copy, line);
              for (char *tok = strtok(copy, " \t"); tok && n < 16; tok = strtok(NULL, " \t")) fields[n++] = tok;
              if (n < 2) continue;
              /* NIS values are the source lines, fields separated by tabs. */
              value[0] = 0;
              for (int i = 0; i < n; i++) {
                  if (i) strcat(value, i == 1 ? "\t" : " ");
                  strcat(value, fields[i]);
              }
              if (suffix_from_field2) {
                  /* services.byname is keyed "port/proto"; services.byservicename "name/proto". */
                  char key[256];
                  const char *slash = strchr(fields[1], '/');
                  if (!slash) continue;
                  if (strcmp(suffix_from_field2, "port") == 0) snprintf(key, sizeof key, "%s", fields[1]);
                  else snprintf(key, sizeof key, "%s%s", fields[0], slash);
                  map_add(m, key, value);
              } else if (all_names) {
                  for (int i = key_field; i < n; i++) map_add(m, fields[i], value);
              } else {
                  map_add(m, fields[key_field], value);
              }
          }
          fclose(f);
      }

      static const char *const map_names[] = {
          "passwd.byname", "passwd.byuid", "group.byname", "group.bygid", "hosts.byname", "hosts.byaddr",
          "services.byname", "services.byservicename", "protocols.byname", "protocols.bynumber",
          "networks.byname", "networks.byaddr", "ypservers", NULL
      };

      /* The map's entries sorted by key, or 0 if there is no such map. */
      static int build_map(const char *name, struct map *m)
      {
          memset(m, 0, sizeof *m);
          if (!strcmp(name, "passwd.byname")) build_passwd(m, 0);
          else if (!strcmp(name, "passwd.byuid")) build_passwd(m, 1);
          else if (!strcmp(name, "group.byname")) build_group(m, 0);
          else if (!strcmp(name, "group.bygid")) build_group(m, 1);
          else if (!strcmp(name, "hosts.byname")) build_etc(m, "hosts", 1, 1, NULL);
          else if (!strcmp(name, "hosts.byaddr")) build_etc(m, "hosts", 0, 0, NULL);
          else if (!strcmp(name, "services.byname")) build_etc(m, "services", 0, 0, "port");
          else if (!strcmp(name, "services.byservicename")) build_etc(m, "services", 0, 0, "name");
          else if (!strcmp(name, "protocols.byname")) build_etc(m, "protocol", 0, 1, NULL);
          else if (!strcmp(name, "protocols.bynumber")) build_etc(m, "protocol", 1, 0, NULL);
          else if (!strcmp(name, "networks.byname")) build_etc(m, "networks", 0, 0, NULL);
          else if (!strcmp(name, "networks.byaddr")) build_etc(m, "networks", 1, 0, NULL);
          else if (!strcmp(name, "ypservers")) {
              char host[256];
              DWORD size = sizeof host;
              if (GetComputerNameExA(ComputerNameDnsHostname, host, &size)) {
                  for (char *c = host; *c; c++) *c = (char)tolower((unsigned char)*c);
                  map_add(m, host, host);
              }
          } else return 0;
          if (m->count > 1) qsort(m->items, (size_t)m->count, sizeof *m->items, compare_entry);
          return 1;
      }

      static int check_args(const char *domain, const char *map)
      {
          if (!domain || !domain[0] || strlen(domain) > YPMAXDOMAIN || (map && (!map[0] || strlen(map) > YPMAXMAP)))
              return YPERR_BADARGS;
          return YPERR_SUCCESS;
      }

      /* NIS hands back copies with "\n\0" after the data; the length excludes both. */
      static char *nis_copy(const char *s, int len)
      {
          char *out = (char *)malloc((size_t)len + 2);
          if (!out) return NULL;
          memcpy(out, s, (size_t)len);
          out[len] = '\n';
          out[len + 1] = 0;
          return out;
      }

      int yp_get_default_domain(char **outdomain)
      {
          static char domain[YPMAXDOMAIN + 1];
          LPWSTR name = NULL;
          NETSETUP_JOIN_STATUS status;
          if (!outdomain) return YPERR_BADARGS;
          if (!domain[0]) {
              if (NetGetJoinInformation(NULL, &name, &status) == NERR_Success) {
                  if (status == NetSetupDomainName || status == NetSetupWorkgroupName) utf8(name, domain, sizeof domain);
                  NetApiBufferFree(name);
              }
          }
          if (!domain[0]) return YPERR_NODOM;
          *outdomain = domain;
          return YPERR_SUCCESS;
      }

      int __yp_check(char **domain)
      {
          char *d;
          if (yp_get_default_domain(&d) != YPERR_SUCCESS) return 0;
          if (domain) *domain = d;
          return 1;
      }

      int yp_bind(const char *domain) { return check_args(domain, NULL); }
      void yp_unbind(const char *domain) { (void)domain; }

      int yp_match(const char *indomain, const char *inmap, const char *inkey, const int inkeylen, char **outval, int *outvallen)
      {
          struct map m;
          int err = check_args(indomain, inmap);
          if (err) return err;
          if (!inkey || inkeylen <= 0 || !outval || !outvallen) return YPERR_BADARGS;
          *outval = NULL;
          *outvallen = 0;
          if (!build_map(inmap, &m)) return YPERR_MAP;
          err = YPERR_KEY;
          for (int i = 0; i < m.count; i++)
              if ((int)strlen(m.items[i].key) == inkeylen && memcmp(m.items[i].key, inkey, (size_t)inkeylen) == 0) {
                  *outvallen = (int)strlen(m.items[i].val);
                  *outval = nis_copy(m.items[i].val, *outvallen);
                  err = *outval ? YPERR_SUCCESS : YPERR_RESRC;
                  break;
              }
          map_free(&m);
          return err;
      }

      static int give_entry(struct entry *e, char **outkey, int *outkeylen, char **outval, int *outvallen)
      {
          *outkeylen = (int)strlen(e->key);
          *outvallen = (int)strlen(e->val);
          *outkey = nis_copy(e->key, *outkeylen);
          *outval = nis_copy(e->val, *outvallen);
          if (*outkey && *outval) return YPERR_SUCCESS;
          free(*outkey);
          free(*outval);
          *outkey = *outval = NULL;
          return YPERR_RESRC;
      }

      int yp_first(const char *indomain, const char *inmap, char **outkey, int *outkeylen, char **outval, int *outvallen)
      {
          struct map m;
          int err = check_args(indomain, inmap);
          if (err) return err;
          if (!outkey || !outkeylen || !outval || !outvallen) return YPERR_BADARGS;
          if (!build_map(inmap, &m)) return YPERR_MAP;
          err = m.count ? give_entry(&m.items[0], outkey, outkeylen, outval, outvallen) : YPERR_NOMORE;
          map_free(&m);
          return err;
      }

      int yp_next(const char *indomain, const char *inmap, const char *inkey, const int inkeylen, char **outkey, int *outkeylen,
                  char **outval, int *outvallen)
      {
          struct map m;
          int err = check_args(indomain, inmap);
          if (err) return err;
          if (!inkey || inkeylen <= 0 || !outkey || !outkeylen || !outval || !outvallen) return YPERR_BADARGS;
          if (!build_map(inmap, &m)) return YPERR_MAP;
          err = YPERR_KEY;
          for (int i = 0; i < m.count; i++)
              if ((int)strlen(m.items[i].key) == inkeylen && memcmp(m.items[i].key, inkey, (size_t)inkeylen) == 0) {
                  /* Duplicate keys (a host listed twice) are passed over in order. */
                  int j = i + 1;
                  err = j < m.count ? give_entry(&m.items[j], outkey, outkeylen, outval, outvallen) : YPERR_NOMORE;
                  break;
              }
          map_free(&m);
          return err;
      }

      int yp_all(const char *indomain, const char *inmap, const struct ypall_callback *incallback)
      {
          struct map m;
          int err = check_args(indomain, inmap);
          if (err) return err;
          if (!incallback || !incallback->foreach) return YPERR_BADARGS;
          if (!build_map(inmap, &m)) return YPERR_MAP;
          int stopped = 0;
          for (int i = 0; i < m.count && !stopped; i++)
              stopped = incallback->foreach(YP_TRUE, m.items[i].key, (int)strlen(m.items[i].key), m.items[i].val,
                                            (int)strlen(m.items[i].val), incallback->data);
          if (!stopped) incallback->foreach(YP_NOMORE, NULL, 0, NULL, 0, incallback->data);
          map_free(&m);
          return YPERR_SUCCESS;
      }

      int yp_master(const char *indomain, const char *inmap, char **outname)
      {
          struct map m;
          char host[256];
          DWORD size = sizeof host;
          int err = check_args(indomain, inmap);
          if (err) return err;
          if (!outname) return YPERR_BADARGS;
          if (!build_map(inmap, &m)) return YPERR_MAP;
          map_free(&m);
          if (!GetComputerNameExA(ComputerNameDnsHostname, host, &size)) return YPERR_YPERR;
          for (char *c = host; *c; c++) *c = (char)tolower((unsigned char)*c);
          *outname = _strdup(host);
          return *outname ? YPERR_SUCCESS : YPERR_RESRC;
      }

      int yp_order(const char *indomain, const char *inmap, unsigned int *outorder)
      {
          struct map m;
          int err = check_args(indomain, inmap);
          if (err) return err;
          if (!outorder) return YPERR_BADARGS;
          if (!build_map(inmap, &m)) return YPERR_MAP;
          map_free(&m);
          /* Maps are rebuilt on every request, so their order number is the current time. */
          *outorder = (unsigned int)time(NULL);
          return YPERR_SUCCESS;
      }

      int yp_maplist(const char *indomain, struct ypmaplist **outmaplist)
      {
          struct ypmaplist *head = NULL;
          int err = check_args(indomain, NULL);
          if (err) return err;
          if (!outmaplist) return YPERR_BADARGS;
          for (int i = (int)(sizeof map_names / sizeof *map_names) - 2; i >= 0; i--) {
              struct ypmaplist *node = (struct ypmaplist *)malloc(sizeof *node);
              if (!node) return YPERR_RESRC;
              node->map = _strdup(map_names[i]);
              node->next = head;
              head = node;
          }
          *outmaplist = head;
          return YPERR_SUCCESS;
      }

      int yp_update(char *domain, char *map, unsigned ypop, char *key, int keylen, char *data, int datalen)
      {
          (void)domain; (void)map; (void)ypop; (void)key; (void)keylen; (void)data; (void)datalen;
          /* Accounts and the etc files are changed with Windows' own tools, not through NIS. */
          return YPERR_ACCESS;
      }

      const char *yperr_string(const int error)
      {
          switch (error) {
          case YPERR_SUCCESS: return "Success";
          case YPERR_BADARGS: return "Request arguments bad";
          case YPERR_RPC: return "RPC failure on NIS operation";
          case YPERR_DOMAIN: return "Can't bind to server which serves this domain";
          case YPERR_MAP: return "No such map in server's domain";
          case YPERR_KEY: return "No such key in map";
          case YPERR_YPERR: return "Internal NIS error";
          case YPERR_RESRC: return "Local resource allocation failure";
          case YPERR_NOMORE: return "No more records in map database";
          case YPERR_PMAP: return "Can't communicate with portmapper";
          case YPERR_YPBIND: return "Can't communicate with ypbind";
          case YPERR_YPSERV: return "Can't communicate with ypserv";
          case YPERR_NODOM: return "Local domain name not set";
          case YPERR_BADDB: return "NIS map database is bad";
          case YPERR_VERS: return "NIS client/server version mismatch - can't supply service";
          case YPERR_ACCESS: return "Permission denied";
          case YPERR_BUSY: return "Database is busy";
          default: return "Unknown NIS error code";
          }
      }

      const char *ypbinderr_string(const int error)
      {
          switch (error) {
          case 0: return "Success";
          case YPBIND_ERR_ERR: return "Internal ypbind error";
          case YPBIND_ERR_NOSERV: return "Domain not bound";
          case YPBIND_ERR_RESC: return "System resource allocation failure";
          default: return "Unknown ypbind error";
          }
      }

      int ypprot_err(const int code)
      {
          switch (code) {
          case YP_TRUE: return YPERR_SUCCESS;
          case YP_NOMORE: return YPERR_NOMORE;
          case YP_FALSE: return YPERR_YPERR;
          case YP_NOMAP: return YPERR_MAP;
          case YP_NODOM: return YPERR_DOMAIN;
          case YP_NOKEY: return YPERR_KEY;
          case YP_BADOP: return YPERR_YPERR;
          case YP_BADDB: return YPERR_BADDB;
          case YP_YPERR: return YPERR_YPERR;
          case YP_BADARGS: return YPERR_BADARGS;
          case YP_VERS: return YPERR_VERS;
          default: return YPERR_YPERR;
          }
      }
    C
    (buildpath/"windows/yptools.c").write <<~'C'
      /* ypcat, ypmatch, ypwhich and ypdomainname (yp-tools' clients) on libnsl; one program that acts on
       * the name it is installed under. */
      #include <rpcsvc/ypclnt.h>
      #include <rpcsvc/yp_prot.h>

      #include <stdio.h>
      #include <stdlib.h>
      #include <string.h>

      static const char *const nicknames[][2] = {
          { "passwd", "passwd.byname" }, { "group", "group.byname" }, { "networks", "networks.byaddr" },
          { "hosts", "hosts.byname" }, { "protocols", "protocols.bynumber" }, { "services", "services.byname" },
          { "aliases", "mail.aliases" }, { "ethers", "ethers.byname" },
      };

      static const char *resolve(const char *map, int translate)
      {
          if (translate)
              for (size_t i = 0; i < sizeof nicknames / sizeof *nicknames; i++)
                  if (strcmp(map, nicknames[i][0]) == 0) return nicknames[i][1];
          return map;
      }

      static void print_nicknames(void)
      {
          for (size_t i = 0; i < sizeof nicknames / sizeof *nicknames; i++)
              printf("Use \"%s\"\tfor map \"%s\"\n", nicknames[i][0], nicknames[i][1]);
      }

      static const char *domain_or_default(const char *domain, const char *prog)
      {
          char *d;
          int err;
          if (domain) return domain;
          if ((err = yp_get_default_domain(&d)) != YPERR_SUCCESS) {
              fprintf(stderr, "%s: can't get local yp domain: %s\n", prog, yperr_string(err));
              exit(1);
          }
          return d;
      }

      static int show_keys;

      static int print_entry(int status, char *key, int keylen, char *val, int vallen, char *data)
      {
          (void)data;
          if (status != YP_TRUE) return 1;
          if (show_keys) printf("%.*s ", keylen, key);
          printf("%.*s\n", vallen, val);
          return 0;
      }

      static int ypcat(int argc, char **argv)
      {
          const char *domain = NULL, *map = NULL;
          int translate = 1;
          for (int i = 1; i < argc; i++) {
              if (!strcmp(argv[i], "-h") || !strcmp(argv[i], "--help")) {
                  puts("Usage: ypcat [-kt] [-d domain] mapname\n       ypcat -x\nPrint the values of all keys in an NIS map.");
                  return 0;
              }
              if (!strcmp(argv[i], "-x")) { print_nicknames(); return 0; }
              if (!strcmp(argv[i], "-k")) show_keys = 1;
              else if (!strcmp(argv[i], "-t")) translate = 0;
              else if (!strcmp(argv[i], "-d") && i + 1 < argc) domain = argv[++i];
              else if (argv[i][0] == '-' || map) { fprintf(stderr, "Usage: ypcat [-kt] [-d domain] mapname\n"); return 2; }
              else map = argv[i];
          }
          if (!map) { fprintf(stderr, "ypcat: no map name given\n"); return 2; }
          struct ypall_callback cb = { print_entry, NULL };
          domain = domain_or_default(domain, "ypcat");
          int err = yp_all(domain, resolve(map, translate), &cb);
          if (err) {
              fprintf(stderr, "No such map %s. Reason: %s\n", map, yperr_string(err));
              return 1;
          }
          return 0;
      }

      static int ypmatch(int argc, char **argv)
      {
          const char *domain = NULL, *keys[64];
          int nkeys = 0, translate = 1, status = 0;
          for (int i = 1; i < argc; i++) {
              if (!strcmp(argv[i], "-h") || !strcmp(argv[i], "--help")) {
                  puts("Usage: ypmatch [-kt] [-d domain] key ... mapname\n       ypmatch -x\nPrint the values of keys in an NIS map.");
                  return 0;
              }
              if (!strcmp(argv[i], "-x")) { print_nicknames(); return 0; }
              if (!strcmp(argv[i], "-k")) show_keys = 1;
              else if (!strcmp(argv[i], "-t")) translate = 0;
              else if (!strcmp(argv[i], "-d") && i + 1 < argc) domain = argv[++i];
              else if (argv[i][0] == '-') { fprintf(stderr, "Usage: ypmatch [-kt] [-d domain] key ... mapname\n"); return 2; }
              else if (nkeys < 64) keys[nkeys++] = argv[i];
          }
          if (nkeys < 2) { fprintf(stderr, "Usage: ypmatch [-kt] [-d domain] key ... mapname\n"); return 2; }
          const char *map = resolve(keys[--nkeys], translate);
          domain = domain_or_default(domain, "ypmatch");
          for (int k = 0; k < nkeys; k++) {
              char *val;
              int vallen, err = yp_match(domain, map, keys[k], (int)strlen(keys[k]), &val, &vallen);
              if (err) {
                  fprintf(stderr, "Can't match key %s in map %s. Reason: %s\n", keys[k], map, yperr_string(err));
                  status = 1;
                  continue;
              }
              if (show_keys) printf("%s ", keys[k]);
              printf("%.*s\n", vallen, val);
              free(val);
          }
          return status;
      }

      static int ypwhich(int argc, char **argv)
      {
          const char *domain = NULL, *map = NULL;
          int maps = 0, translate = 1;
          for (int i = 1; i < argc; i++) {
              if (!strcmp(argv[i], "-h") || !strcmp(argv[i], "--help")) {
                  puts("Usage: ypwhich [-d domain]\n       ypwhich [-d domain] -m [mapname]\n       ypwhich -x\n"
                       "Print the NIS server (this computer) or the master of maps.");
                  return 0;
              }
              if (!strcmp(argv[i], "-x")) { print_nicknames(); return 0; }
              if (!strcmp(argv[i], "-m")) maps = 1;
              else if (!strcmp(argv[i], "-t")) translate = 0;
              else if (!strcmp(argv[i], "-d") && i + 1 < argc) domain = argv[++i];
              else if (argv[i][0] != '-' && maps && !map) map = argv[i];
              else { fprintf(stderr, "Usage: ypwhich [-d domain] [-m [mapname]]\n"); return 2; }
          }
          domain = domain_or_default(domain, "ypwhich");
          if (maps) {
              struct ypmaplist *list = NULL;
              if (map) {
                  char *master;
                  int err = yp_master(domain, resolve(map, translate), &master);
                  if (err) { fprintf(stderr, "Can't find master for map %s. Reason: %s\n", map, yperr_string(err)); return 1; }
                  puts(master);
                  free(master);
                  return 0;
              }
              if (yp_maplist(domain, &list) != YPERR_SUCCESS) return 1;
              for (; list; list = list->next) {
                  char *master;
                  if (yp_master(domain, list->map, &master) == YPERR_SUCCESS) { printf("%s %s\n", list->map, master); free(master); }
              }
              return 0;
          }
          char *master;
          if (yp_master(domain, "ypservers", &master) != YPERR_SUCCESS) return 1;
          puts(master);
          free(master);
          return 0;
      }

      static int ypdomainname(int argc, char **argv)
      {
          char *domain;
          if (argc > 1 && (!strcmp(argv[1], "-h") || !strcmp(argv[1], "--help"))) {
              puts("Usage: ypdomainname\nPrint the NIS domain (the Windows domain or workgroup).");
              return 0;
          }
          if (argc > 1) { fprintf(stderr, "ypdomainname: setting the domain is done in Windows' System settings\n"); return 1; }
          if (yp_get_default_domain(&domain) != YPERR_SUCCESS) {
              fprintf(stderr, "ypdomainname: Local domain name not set\n");
              return 1;
          }
          puts(domain);
          return 0;
      }

      int main(int argc, char **argv)
      {
          const char *name = argv[0], *p;
          char base[64];
          for (p = argv[0]; *p; p++) if (*p == '/' || *p == '\\') name = p + 1;
          snprintf(base, sizeof base, "%s", name);
          char *dot = strrchr(base, '.');
          if (dot && _stricmp(dot, ".exe") == 0) *dot = 0;
          if (!_stricmp(base, "ypcat")) return ypcat(argc, argv);
          if (!_stricmp(base, "ypmatch")) return ypmatch(argc, argv);
          if (!_stricmp(base, "ypwhich")) return ypwhich(argc, argv);
          return ypdomainname(argc, argv);
      }
    C
    cd "windows" do
      system ENV.cc, "-O2", "-shared", "-I.", "-o", "libnsl-3.dll", "nsl.c",
             "-Wl,--out-implib,libnsl.dll.a", "-lnetapi32", "-ladvapi32"
      %w[ypcat ypmatch ypwhich ypdomainname].each do |tool|
        system ENV.cc, "-O2", "-I.", "-o", "#{tool}.exe", "yptools.c", "-L.", "-lnsl"
      end
      (include/"rpcsvc").install "rpcsvc/ypclnt.h", "rpcsvc/yp_prot.h"
      lib.install "libnsl.dll.a"
      bin.install "libnsl-3.dll", "ypcat.exe", "ypmatch.exe", "ypwhich.exe", "ypdomainname.exe"
    end
    (lib/"pkgconfig/libnsl.pc").write <<~PC
      prefix=${pcfiledir}/../..
      libdir=${prefix}/lib
      includedir=${prefix}/include

      Name: libnsl
      Description: Library containing NIS functions (Windows account and etc-file maps)
      Version: #{version}
      Cflags: -I${includedir}
      Libs: -L${libdir} -lnsl
    PC
    doc.install "COPYING"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # use the yp tools built on the library instead.
    domain = shell_output("#{bin}/ypdomainname").chomp
    refute_empty domain
    assert_match ":*:500:", shell_output("#{bin}/ypmatch 500 passwd.byuid")
    assert_match "Administrators:*:544:", shell_output("#{bin}/ypcat group")
    assert_match "No such map", shell_output("#{bin}/ypcat nosuchmap 2>&1", 1)
    assert_match "passwd.byname", shell_output("#{bin}/ypwhich -m")
  end
end
