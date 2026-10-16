class Cymbal < Formula
  desc "Language-agnostic code navigation CLI powered by tree-sitter"
  homepage "https://github.com/1broseidon/cymbal"
  url "https://github.com/1broseidon/cymbal/releases/download/v0.17.0/cymbal_v0.17.0_windows_x86_64.zip"
  sha256 "0f03fe94be417496686f95836bb11ecfa74ec065b9d6ec3859454efec06d5d4c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/1broseidon/cymbal/archive/refs/tags/v0.17.0.tar.gz"
  end

  def install
    bin.install "cymbal.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cymbal --version 2>&1")
  end
end
