class SpytrapAdb < Formula
  desc "Test a phone for stalkerware and suspicious configuration using usb debugging"
  homepage "https://github.com/spytrap-org/spytrap-adb"
  url "https://github.com/spytrap-org/spytrap-adb/releases/download/v0.3.5/spytrap-adb-0.3.5.tar.gz"
  sha256 "e33a342aa461b56a8d857c2c29b743658bd87b4ade7bed20399dea2cea01b0f8"
  license "GPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a302755e0c5416f30a7b96e027c0127b83aef80262ff6db147d54edd6bc3eff0"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args

    generate_completions_from_executable(bin/"spytrap-adb", "completions")
  end

  def caveats
    <<~EOS
      At runtime, adb must be accessible from your PATH.

      You can install adb from Homebrew Cask:
        brew install --cask android-platform-tools
    EOS
  end

  test do
    system bin/"spytrap-adb", "download-ioc"
  end
end
