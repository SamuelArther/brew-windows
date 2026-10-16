class Dependabot < Formula
  desc "Tool for testing and debugging Dependabot update jobs"
  homepage "https://github.com/dependabot/cli"
  url "https://github.com/dependabot/cli/releases/download/v1.95.0/dependabot-v1.95.0-windows-amd64.zip"
  sha256 "e4c58ba204f1095ea5a991b439923f93e3fcaf62ad2afe65dc901d9252e378a9"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dependabot/cli/archive/refs/tags/v1.95.0.tar.gz"
  end

  def install
    bin.install "dependabot.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dependabot --version 2>&1")
  end
end
