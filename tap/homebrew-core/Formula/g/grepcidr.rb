class Grepcidr < Formula
  desc "Filter IP addresses matching IPv4 CIDR/network specification"
  homepage "https://www.pc-tools.net/unix/grepcidr/"
  url "https://www.pc-tools.net/files/unix/grepcidr-2.0.tar.gz"
  sha256 "61886a377dabf98797145c31f6ba95e6837b6786e70c932324b7d6176d50f7fb"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?grepcidr[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/grepcidr-2.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d361691f918551d01aaa208e869a3dc70043ccc8946d42684bda4cc83833409c"
  end

  def install
    system "make"
    bin.install "grepcidr"
    man1.install "grepcidr.1"
  end

  test do
    (testpath/"access.log").write <<~EOS
      127.0.0.1 duck
      8.8.8.8 duck
      66.249.64.123 goose
      192.168.0.1 duck
    EOS

    output = shell_output("#{bin}/grepcidr 66.249.64.0/19 access.log")
    assert_equal "66.249.64.123 goose", output.strip
  end
end
