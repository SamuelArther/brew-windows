class Fselect < Formula
  desc "Find files with SQL-like queries"
  homepage "https://fselect.rocks"
  url "https://github.com/jhspetersson/fselect/releases/download/0.10.3/fselect-x86_64-win.zip"
  sha256 "b96f46a8cde6c3671db71d4be3b82fe6b0c44cd25c4581f0fad3ec6f07ad9d8a"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jhspetersson/fselect/archive/refs/tags/0.10.3.tar.gz"
  end

  def install
    bin.install "fselect.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fselect --version 2>&1")
  end
end
