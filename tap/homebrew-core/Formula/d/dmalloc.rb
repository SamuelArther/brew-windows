class Dmalloc < Formula
  desc "Debug versions of system memory management routines"
  homepage "https://dmalloc.com/"
  url "https://dmalloc.com/releases/dmalloc-5.6.5.tgz"
  sha256 "480e3414ab6cedca837721c756b7d64b01a84d2d0e837378d98444e2f63a7c01"
  license "ISC"

  livecheck do
    url "https://dmalloc.com/releases/"
    regex(/href=.*?dmalloc[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/dmalloc-5.6.5"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8a1172a18eb6a006046d1c68c30277e8a342b311abca312eec8a12acfc9d2c44"
  end

  def install
    system "./configure", "--enable-threads", "--prefix=#{prefix}"
    system "make", "install", "installth", "installcxx", "installthcxx"
  end

  test do
    system bin/"dmalloc", "-b", "runtime"
  end
end
