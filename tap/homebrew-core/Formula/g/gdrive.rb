class Gdrive < Formula
  desc "Google Drive CLI Client"
  homepage "https://github.com/glotlabs/gdrive"
  url "https://github.com/glotlabs/gdrive/releases/download/3.9.1/gdrive_windows-x64.zip"
  sha256 "4bb6b47c419343ef9955acbf22bf615154d5cce0af45400b36c3563b9a1be34d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/glotlabs/gdrive/archive/refs/tags/3.9.1.tar.gz"
  end

  def install
    bin.install "gdrive.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gdrive version")
    assert_match "Usage: gdrive <COMMAND>", shell_output("#{bin}/gdrive 2>&1", 2)
    assert_match "Error: No accounts found", shell_output("#{bin}/gdrive account list 2>&1", 1)
  end
end
