class GoogleworkspaceCli < Formula
  desc "CLI for Drive, Gmail, Calendar, Sheets, Docs, Chat, Admin, and more"
  homepage "https://developers.google.com/workspace"
  url "https://github.com/googleworkspace/cli/releases/download/v0.22.5/google-workspace-cli-x86_64-pc-windows-msvc.zip"
  sha256 "407705d695dc83d48b1c5f50d71b5aa64095bf6f17d5b439b2e9a373bbe67ec2"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/googleworkspace/cli/archive/refs/tags/v0.22.5.tar.gz"
  end

  def install
    bin.install "gws.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gws --version 2>&1")
  end
end
