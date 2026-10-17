class Roundup < Formula
  desc "Unit testing tool"
  homepage "https://bmizerany.github.io/roundup"
  url "https://github.com/bmizerany/roundup/archive/refs/tags/v0.0.6.tar.gz"
  sha256 "20741043ed5be7cbc54b1e9a7c7de122a0dacced77052e90e4ff08e41736f01c"
  license "MIT"
  head "https://github.com/bmizerany/roundup.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/roundup-0.0.6"
    sha256 cellar: :any, x86_64_windows: "46faf433731bd7692be050e229f228ae7fb5c66ade75f82eb4a7dbec5cc6c67f"
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
