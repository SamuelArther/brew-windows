class Clac < Formula
  desc "Command-line, stack-based calculator with postfix notation"
  homepage "https://github.com/soveran/clac"
  url "https://github.com/soveran/clac/archive/refs/tags/0.3.5.tar.gz"
  sha256 "ae0a989ba3efc5dbe8480aed5fe39e79e274ea51271bfa7a0193d73cca0b9711"
  license "BSD-2-Clause"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/clac-0.3.5"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "711ae9017003dc3a124442a3e65bfd50eb718a654249731ce46ac02093d0223b"
  end

  def install
    system "make", "PREFIX=#{prefix}", "install"
  end

  test do
    assert_equal "7", shell_output("#{bin}/clac '3 4 +'").strip
  end
end
