class Ifstat < Formula
  desc "Tool to report network interface bandwidth"
  homepage "http://gael.roualland.free.fr/ifstat/"
  # Upstream is only available via HTTP, so we prefer Debian's HTTPS mirror
  url "https://deb.debian.org/debian/pool/main/i/ifstat/ifstat_1.1.orig.tar.gz"
  mirror "http://gael.roualland.free.fr/ifstat/ifstat-1.1.tar.gz"
  sha256 "8599063b7c398f9cfef7a9ec699659b25b1c14d2bc0f535aed05ce32b7d9f507"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=["']?ifstat[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "21f5dc7da74dce1923fed5fa96542cd8180c32d7373879fe463231b390797e6e"
  end

  # Fixes 32/64 bit incompatibility
  patch :DATA

  deny_network_access!

  def install
    # Fix compile with newer Clang
    if DevelopmentTools.clang_build_version >= 1403
      ENV.append_to_cflags "-Wno-implicit-function-declaration -Wno-implicit-int"
    end

    system "./configure", "--disable-debug", "--disable-dependency-tracking",
                          "--prefix=#{prefix}",
                          "--mandir=#{man}"
    system "make", "install"
  end

  test do
    system bin/"ifstat", "-v"
  end
end

__END__
diff --git a/drivers.c b/drivers.c
index d5ac501..47fb320 100644
--- a/drivers.c
+++ b/drivers.c
@@ -593,7 +593,8 @@ static int get_ifcount() {
   int ifcount[] = {
     CTL_NET, PF_LINK, NETLINK_GENERIC, IFMIB_SYSTEM, IFMIB_IFCOUNT
   };
-  int count, size;
+  int count;
+  size_t size;
   
   size = sizeof(count);
   if (sysctl(ifcount, sizeof(ifcount) / sizeof(int), &count, &size, NULL, 0) < 0) {
@@ -607,7 +608,7 @@ static int get_ifdata(int index, struct ifmibdata * ifmd) {
   int ifinfo[] = {
     CTL_NET, PF_LINK, NETLINK_GENERIC, IFMIB_IFDATA, index, IFDATA_GENERAL
   };
-  int size = sizeof(*ifmd);
+  size_t size = sizeof(*ifmd);
 
   if (sysctl(ifinfo, sizeof(ifinfo) / sizeof(int), ifmd, &size, NULL, 0) < 0)
     return 0;
