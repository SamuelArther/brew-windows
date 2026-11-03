class Libesedb < Formula
  desc "Library and tools for Extensible Storage Engine (ESE) Database files"
  homepage "https://github.com/libyal/libesedb"
  url "https://github.com/libyal/libesedb/releases/download/20260704/libesedb-experimental-20260704.tar.gz"
  sha256 "78f5e4cd11b551e673db270a9d40abf3c8ec8523b91939a9251ca80ee5a83bd1"
  license "LGPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fe2c11e3730027281fd93641737865eb17879a14be97f520ece8fb8a08df3771"
  end

  depends_on "pkgconf" => [:build, :test]

  deny_network_access!

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/esedbinfo -V")

    (testpath/"test.c").write <<~C
      #include <libesedb.h>
      #include <stdio.h>

      int main() {
        printf("libesedb version: %d\\n", LIBESEDB_VERSION);
        return 0;
      }
    C

    flags = shell_output("pkgconf --cflags --libs libesedb").chomp.split
    system ENV.cc, "test.c", "-o", "test", *flags
    assert_match "libesedb version: #{version}", shell_output("./test")
  end
end
