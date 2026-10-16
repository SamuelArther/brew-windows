class Committed < Formula
  desc "Nitpicking commit history since beabf39"
  homepage "https://github.com/crate-ci/committed"
  url "https://github.com/crate-ci/committed/releases/download/v1.1.11/committed-v1.1.11-x86_64-pc-windows-msvc.zip"
  sha256 "af4f5a65320471751ed1dfbb5502f15f4857cea797b077c6049acb2b995d293b"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/crate-ci/committed/archive/refs/tags/v1.1.11.tar.gz"
  end

  def install
    bin.install "committed.exe"
  end

  test do
    system "git", "init"
    system "git", "-c", "user.email=a@b.c", "-c", "user.name=t",
                  "commit", "--allow-empty", "-m", "bad message"
    output = shell_output("#{bin}/committed HEAD 2>&1", 1)
    assert_match "Subject", output
  end
end
