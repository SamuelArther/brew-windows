class Code2prompt < Formula
  desc "CLI tool to convert your codebase into a single LLM prompt"
  homepage "https://code2prompt.dev/"
  url "https://github.com/mufeedvh/code2prompt/releases/download/v4.2.0/code2prompt-x86_64-pc-windows-msvc.exe"
  sha256 "e3285c8b7319659c9e6fda4b63a1cb5d83e07b1688df83ed224fdb801d9da7d1"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mufeedvh/code2prompt/archive/refs/tags/v4.2.0.tar.gz"
  end

  def install
    bin.install "code2prompt-x86_64-pc-windows-msvc.exe" => "code2prompt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/code2prompt --version 2>&1")
  end
end
