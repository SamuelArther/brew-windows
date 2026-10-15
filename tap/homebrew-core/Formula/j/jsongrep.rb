class Jsongrep < Formula
  desc "Query tool for JSON, YAML, TOML, and other structured formats"
  homepage "https://github.com/micahkepe/jsongrep"
  url "https://github.com/micahkepe/jsongrep/releases/download/v0.10.0/jsongrep-0.10.0-x86_64-pc-windows-msvc.zip"
  sha256 "5a906a6e2a0db04a83507a97282ca9419aa656ebca8e9804139f4c80df95b896"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/micahkepe/jsongrep/archive/refs/tags/v0.10.0.tar.gz"
  end

  def install
    bin.install "jg.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jg --version")

    assert_equal "2\n", pipe_output("#{bin}/jg -F bar", '{"foo":1, "bar":2}')
  end
end
