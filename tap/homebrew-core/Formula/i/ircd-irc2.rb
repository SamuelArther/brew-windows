class IrcdIrc2 < Formula
  desc "Original IRC server daemon"
  homepage "https://web.archive.org/web/20240814044559/http://www.irc.org/"
  url "https://web.archive.org/web/20240805183402/http://www.irc.org/ftp/irc/server/irc2.11.2p3.tgz"
  version "2.11.2p3"
  sha256 "be94051845f9be7da0e558699c4af7963af7e647745d339351985a697eca2c81"
  # The `:cannot_represent` is for a Digital Equipment Corporation license.
  # TODO: See if SPDX will consider this a match for HPND License.
  license all_of: [
    "GPL-1.0-or-later",
    "GPL-2.0-or-later", # ircd/fileio.*, ircd/patricia.c
    "ISC", # ircd/res_comp.c
    "BSD-4-Clause-UC", # ircd/{res_comp.c,res_init.c,res_mkquery.c,resolv_def.h}
    :cannot_represent, # ircd/{res_comp.c,res_init.c,res_mkquery.c,resolv_def.h}
  ]

  def default_ircd_conf
    <<~EOS
      # M-Line
      M:irc.localhost::Darwin ircd default configuration::000A

      # A-Line
      A:This is Darwin's default ircd configurations:Please edit your /usr/local/etc/ircd.conf file:Contact <root@localhost> for questions::ExampleNet

      # Y-Lines
      Y:1:90::100:512000:5.5:100.100
      Y:2:90::300:512000:5.5:250.250

      # I-Line
      I:*:::0:1
      I:127.0.0.1/32:::0:1

      # P-Line
      P::::6667:
    EOS
  end

  # Last release on 2010-08-13 and site is down
  deprecate! date: "2025-04-06", because: :unmaintained
  disable! date: "2026-04-06", because: :unmaintained

  uses_from_macos "libxcrypt"

  on_linux do
    on_arm do
      # Added automake as a build dependency to update config files for ARM support.
      depends_on "automake" => :build
    end
  end

  conflicts_with "ircd-hybrid", because: "both install `ircd` binaries"

  # Replace usage of nameser_def.h which has incompatible IBM license.
  # Ref: https://gitlab.com/fedora/legal/fedora-license-data/-/issues/53
  patch :DATA

  def install
    # Remove header with incompatible IBM license and add linker flags to use system library instead
    rm("ircd/nameser_def.h")
    ENV.append "LIBS", "-lresolv"

    if OS.linux? && Hardware::CPU.arm?
      # Workaround for ancient config files not recognizing aarch64 macos.
      %w[config.guess config.sub].each do |fn|
        cp Formula["automake"].share/"automake-#{Formula["automake"].version.major_minor}"/fn, "support/#{fn}"
      end
    end

    system "./configure", "--prefix=#{prefix}",
                          "--localstatedir=#{var}",
                          "--sysconfdir=#{etc}",
                          "--mandir=#{man}",
                          "CFLAGS=-DRLIMIT_FDMAX=0"

    build_dir = `./support/config.guess`.chomp

    # Disable netsplit detection. In a netsplit, joins to new channels do not
    # give chanop status.
    inreplace "#{build_dir}/config.h", /#define DEFAULT_SPLIT_USERS\s+65000/,
      "#define DEFAULT_SPLIT_USERS 0"
    inreplace "#{build_dir}/config.h", /#define DEFAULT_SPLIT_SERVERS\s+80/,
      "#define DEFAULT_SPLIT_SERVERS 0"

    # The directory is something like `i686-apple-darwin13.0.2'
    system "make", "install", "-C", build_dir

    (buildpath/"ircd.conf").write default_ircd_conf
    etc.install "ircd.conf"
  end

  service do
    run [opt_sbin/"ircd", "-t"]
    keep_alive false
    working_dir HOMEBREW_PREFIX
    error_log_path var/"ircd.log"
  end

  test do
    system sbin/"ircd", "-version"
  end
end

__END__
diff --git a/ircd/res_comp.c b/ircd/res_comp.c
index b58d06c..a0ff2b9 100644
--- a/ircd/res_comp.c
+++ b/ircd/res_comp.c
@@ -64,6 +64,7 @@ static const volatile char rcsid[] = "$Id: res_comp.c,v 1.10 2004/10/01 20:22:14
 #include "s_externs.h"
 #undef RES_COMP_C

+#if 0
 static int	ns_name_ntop (const u_char *, char *, size_t);
 static int	ns_name_pton (const char *, u_char *, size_t);
 static int	ns_name_unpack (const u_char *, const u_char *,
@@ -75,6 +76,7 @@ static int	ns_name_uncompress (const u_char *, const u_char *,
 static int	ns_name_compress (const char *, u_char *, size_t,
 				      const u_char **, const u_char **);
 static int	ns_name_skip (const u_char **, const u_char *);
+#endif

 /*
  * Expand compressed domain name 'comp_dn' to full domain name.
@@ -306,6 +308,7 @@ static int		dn_find (const u_char *, const u_char *,

 /* Public. */

+#if 0
 /*
  * ns_name_ntop(src, dst, dstsiz)
  *	Convert an encoded domain name to printable ascii as per RFC1035.
@@ -749,6 +752,7 @@ static int	ns_name_skip(const u_char **ptrptr, const u_char *eom)
 	*ptrptr = cp;
 	return (0);
 }
+#endif

 /* Private. */

diff --git a/ircd/s_defines.h b/ircd/s_defines.h
index aaaf0d4..acd1378 100644
--- a/ircd/s_defines.h
+++ b/ircd/s_defines.h
@@ -37,4 +37,5 @@
 #include "service_def.h"
 #include "sys_def.h"
 #include "resolv_def.h"
-#include "nameser_def.h"
+#define BIND_8_COMPAT
+#include <arpa/nameser.h>
