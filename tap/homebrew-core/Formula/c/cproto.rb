class Cproto < Formula
  desc "Generate function prototypes for functions in input files"
  homepage "https://invisible-island.net/cproto/"
  url "https://invisible-mirror.net/archives/cproto/cproto-4.8a.tgz"
  mirror "https://deb.debian.org/debian/pool/main/c/cproto/cproto_4.8a.orig.tar.gz"
  sha256 "beb121e08c0d47b5bd719071c32a77edcc31dff992a84e3d9a59c0f7ec9fadd3"
  license :public_domain

  livecheck do
    url "https://invisible-mirror.net/archives/cproto/"
    regex(/href=.*?cproto[._-]v?(\d+(?:\.\d+)+[a-z]?)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "5ae5864ca68c334498be6307606e191f7c0c85cf293ad4c0d8d23c37bd1784b9"
  end

  uses_from_macos "bison" => :build
  uses_from_macos "flex" => :build

  deny_network_access!

  def install
    system "./configure", "--disable-debug",
                          "--disable-dependency-tracking",
                          "--disable-silent-rules",
                          "--prefix=#{prefix}"

    system "make", "install"
  end

  test do
    (testpath/"woot.c").write("int woot() {\n}")
    assert_match(/int woot.void.;/, shell_output("#{bin}/cproto woot.c"))
  end
end
