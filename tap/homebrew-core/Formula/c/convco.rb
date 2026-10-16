class Convco < Formula
  desc "Conventional commits, changelog, versioning, validation"
  homepage "https://convco.github.io"
  url "https://github.com/convco/convco/releases/download/v0.7.2/convco-v0.7.2-x86_64-pc-windows-msvc.tar.gz"
  sha256 "57601b963de1b3a8ffff75041b76e2caa20d375305037ffe120f4fc423ece3e6"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/convco/convco/archive/refs/tags/v0.7.2.tar.gz"
  end

  def install
    bin.install "convco.exe"
  end

  test do
    system "git", "init"
    system "git", "commit", "--allow-empty", "-m", "invalid"
    assert_match(/FAIL  \w+  first line doesn't match `<type>\[optional scope\]: <description>`  invalid\n/,
      shell_output("#{bin}/convco check", 1).lines.first)
  end
end
