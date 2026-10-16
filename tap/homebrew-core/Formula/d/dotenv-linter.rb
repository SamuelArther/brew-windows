class DotenvLinter < Formula
  desc "Lightning-fast linter for .env files written in Rust"
  homepage "https://dotenv-linter.github.io"
  url "https://github.com/dotenv-linter/dotenv-linter/releases/download/v4.0.0/dotenv-linter-win-x64.zip"
  sha256 "771214659b86d2de5c8c5214e6d59484ceb0de8a87037d488ce37d6ca6f6cc1a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dotenv-linter/dotenv-linter/archive/refs/tags/v4.0.0.tar.gz"
  end

  def install
    bin.install "dotenv-linter.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dotenv-linter --version")

    (testpath/".env").write <<~EOS
      FOO=bar
      FOO=bar
      BAR=foo
    EOS

    (testpath/".env.test").write <<~EOS
      1FOO=bar
      _FOO=bar
    EOS

    output = shell_output("#{bin}/dotenv-linter check .env .env.test", 1)
    assert_match(/\.env:2\s+DuplicatedKey/, output)
    assert_match(/\.env:3\s+UnorderedKey/, output)
    assert_match(/\.env.test:1\s+LeadingCharacter/, output)
  end
end
