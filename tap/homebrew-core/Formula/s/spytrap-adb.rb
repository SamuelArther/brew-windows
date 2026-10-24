class SpytrapAdb < Formula
  desc "Test a phone for stalkerware and suspicious configuration using usb debugging"
  homepage "https://github.com/spytrap-org/spytrap-adb"
  url "https://github.com/spytrap-org/spytrap-adb/releases/download/v0.3.5/spytrap-adb-0.3.5.tar.gz"
  sha256 "e33a342aa461b56a8d857c2c29b743658bd87b4ade7bed20399dea2cea01b0f8"
  license "GPL-3.0-or-later"
  head "https://github.com/spytrap-org/spytrap-adb.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b0ffe7fa41c0e6c1b1701bbdf15d69be5281ca3437f88f7e2c6199d8821ee62b"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

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
