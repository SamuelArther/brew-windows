class Nerdfix < Formula
  desc "Find/fix obsolete Nerd Font icons"
  homepage "https://github.com/loichyan/nerdfix"
  url "https://github.com/loichyan/nerdfix/releases/download/v0.4.2/nerdfix-x86_64-pc-windows-msvc.zip"
  sha256 "207f91fa275f0155160a2ef823c114ed78b9ef857d5a6a94ecbb7b8224f6c0f5"
  license any_of: ["MIT", "Apache-2.0"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/loichyan/nerdfix/archive/refs/tags/v0.4.2.tar.gz"
  end

  def install
    bin.install "nerdfix.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nerdfix --version")

    touch "test.txt"
    system bin/"nerdfix", "check", "test.txt"
  end
end
