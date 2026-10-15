class Bkcrack < Formula
  desc "Crack legacy zip encryption with Biham and Kocher's known plaintext attack"
  homepage "https://github.com/kimci86/bkcrack"
  url "https://github.com/kimci86/bkcrack/releases/download/v1.8.1/bkcrack-1.8.1-win64.zip"
  sha256 "b35ba316170486e827dd41be2a771db06c75a309e660efc87217418149ca498a"
  license "Zlib"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kimci86/bkcrack/archive/refs/tags/v1.8.1.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bkcrack.exe", "bkcrack"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bkcrack --version 2>&1")
  end
end
