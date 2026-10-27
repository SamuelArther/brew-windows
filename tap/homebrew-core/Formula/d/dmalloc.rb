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
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fd06b56de54307d5bc3d5f5871b84c15ba795a2f5df7a6a0abcdac47cd0c2f34"
  end

  deny_network_access!

  def install
    system "./configure", "--enable-threads", "--prefix=#{prefix}"
    system "make", "install", "installth", "installcxx", "installthcxx"
  end

  test do
    system bin/"dmalloc", "-b", "runtime"
  end
end
