class Smug < Formula
  desc "Automate your tmux workflow"
  homepage "https://github.com/ivaaaan/smug"
  url "https://github.com/ivaaaan/smug/releases/download/v0.3.19/smug_0.3.19_Windows_x86_64.zip"
  sha256 "31164ff711b09118549242f5209f179a3e1ea4f6f67ac254d998f556cdb54457"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ivaaaan/smug/archive/refs/tags/v0.3.19.tar.gz"
  end

  def install
    bin.install "smug.exe"
  end

  test do
    system bin/"smug", "--help"
  end
end
