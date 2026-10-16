class Pueue < Formula
  desc "Command-line tool for managing long-running shell commands"
  homepage "https://github.com/Nukesor/pueue"
  url "https://github.com/Nukesor/pueue/releases/download/v4.0.4/pueued-x86_64-pc-windows-msvc.exe"
  sha256 "aafa05e2f26cda9aff3eeb9be261e8f9f67752d1e9bb7fbcb47318e35c52ab1d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Nukesor/pueue/archive/refs/tags/v4.0.4.tar.gz"
  end

  def install
    bin.install "pueued-x86_64-pc-windows-msvc.exe" => "pueue.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pueue --version 2>&1")
  end
end
