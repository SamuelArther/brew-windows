class MacRobber < Formula
  desc "Digital investigation tool"
  homepage "https://www.sleuthkit.org/mac-robber/"
  url "https://downloads.sourceforge.net/project/mac-robber/mac-robber/1.02/mac-robber-1.02.tar.gz"
  sha256 "5895d332ec8d87e15f21441c61545b7f68830a2ee2c967d381773bd08504806d"
  license "GPL-2.0-or-later"

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e5cb7b1e010b6555ebfc38254fa1b3e603a2a834972be9ae471e9b9a5402156c"
  end

  def install
    system "make", "CC=#{ENV.cc}", "GCC_OPT=#{ENV.cflags}"
    bin.install "mac-robber"
  end

  test do
    (testpath/"data").mkpath
    (testpath/"data/hello.txt").write "hello"
    chmod 0644, testpath/"data/hello.txt"
    (testpath/"data/link").make_symlink "hello.txt"

    output = shell_output("#{bin}/mac-robber data")
    assert_match "MD5|name|inode|mode_as_string|UID|GID|size|atime|mtime|ctime|crtime", output
    assert_match %r{^0\|data/hello\.txt\|0\|-rw-r--r--\|\d+\|\d+\|5\|}, output
    assert_match %r{^0\|data/link\|0\|l\S+ -> hello\.txt\|}, output

    assert_match "invalid directory: missing/", shell_output("#{bin}/mac-robber missing", 1)
  end
end
