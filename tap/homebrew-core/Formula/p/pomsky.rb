class Pomsky < Formula
  desc "Regular expression language"
  homepage "https://pomsky-lang.org/"
  url "https://github.com/pomsky-lang/pomsky/releases/download/v0.12.0/pomsky-bin-x86_64-pc-windows-msvc.zip"
  version "0.12"
  sha256 "1598165f442febeccb7e38777219060ff2664c69320d673e50ca7b0416f7e563"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pomsky-lang/pomsky/archive/refs/tags/v0.12.tar.gz"
  end

  def install
    bin.install "pomsky.exe"
  end

  test do
    assert_match "Backslash escapes are not supported",
      shell_output("#{bin}/pomsky \"'Hello world'* \\X+\" 2>&1", 1)

    assert_match version.to_s, shell_output("#{bin}/pomsky --version")
  end
end
