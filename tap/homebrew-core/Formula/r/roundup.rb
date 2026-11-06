class Roundup < Formula
  desc "Unit testing tool"
  homepage "https://bmizerany.github.io/roundup"
  url "https://github.com/bmizerany/roundup/archive/refs/tags/v0.0.6.tar.gz"
  sha256 "20741043ed5be7cbc54b1e9a7c7de122a0dacced77052e90e4ff08e41736f01c"
  license "MIT"
  head "https://github.com/bmizerany/roundup.git", branch: "master"

  deny_network_access!

  bottle do
    sha256 cellar: :any, x86_64_windows: "84bbb5e7fb7d66f0225af179f170a3cd00d37feefefe2714a9b8b74a7618c6ce"
  end

  def install
    system "./configure", "--prefix=#{prefix}", "--sysconfdir=#{etc}"
    system "make", "SHELL=/bin/bash"
    system "make", "install"
  end

  test do
    system bin/"roundup", "-v"
  end
end
