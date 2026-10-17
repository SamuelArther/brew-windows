class Odo < Formula
  desc "Atomic odometer for the command-line"
  homepage "https://github.com/atomicobject/odo"
  url "https://github.com/atomicobject/odo/archive/refs/tags/v0.2.2.tar.gz"
  sha256 "52133a6b92510d27dfe80c7e9f333b90af43d12f7ea0cf00718aee8a85824df5"
  license "ISC"

  conflicts_with "odo-dev", because: "odo-dev also ships 'odo' binary"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/odo-0.2.2"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "bb8e2a85f789056c9387b4bee575dd734599ba3cf6135632d9a33c0941d423f4"
  end

  def install
    system "make"
    man1.mkpath
    bin.mkpath
    system "make", "test"
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    system bin/"odo", "testlog"
  end
end
