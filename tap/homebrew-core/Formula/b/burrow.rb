class Burrow < Formula
  desc "Kafka Consumer Lag Checking"
  homepage "https://github.com/linkedin/Burrow"
  url "https://github.com/linkedin/Burrow/releases/download/v1.9.6/Burrow_1.9.6_windows_amd64.tar.gz"
  sha256 "61eb1696f7cd2138fadc2d1cdd75c22e56a9ca528afbd55f8219cfffbac3967a"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/linkedin/Burrow/archive/refs/tags/v1.9.6.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/burrow.exe", "burrow"
  end

  test do
    system bin/"burrow", "--help"
  end
end
