class Spacer < Formula
  desc "Small command-line utility for adding spacers to command output"
  homepage "https://github.com/samwho/spacer"
  url "https://github.com/samwho/spacer/releases/download/v0.5.0/spacer-x86_64-pc-windows-msvc.zip"
  sha256 "9bc502b79c8c8619693b1fa0d7a92227db5787a874323f1dfb70192c3d051631"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/samwho/spacer/archive/refs/tags/v0.5.0.tar.gz"
  end

  def install
    bin.install "spacer.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/spacer --version 2>&1")
  end
end
