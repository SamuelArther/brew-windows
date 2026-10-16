class Hostess < Formula
  desc "Idempotent command-line utility for managing your /etc/hosts file"
  homepage "https://github.com/cbednarski/hostess"
  url "https://github.com/cbednarski/hostess/releases/download/v0.5.2/hostess_windows_amd64.exe"
  sha256 "44a4f02a3c86f4832070c8b73b2d54cacda48b3c2114679bcde51abc9656127a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cbednarski/hostess/archive/refs/tags/v0.5.2.tar.gz"
  end

  def install
    bin.install "hostess_windows_amd64.exe" => "hostess.exe"
  end

  test do
    assert_match "localhost", shell_output("#{bin}/hostess ls 2>&1")
  end
end
