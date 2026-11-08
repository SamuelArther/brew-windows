class SpytrapAdb < Formula
  desc "Test a phone for stalkerware and suspicious configuration using usb debugging"
  homepage "https://github.com/spytrap-org/spytrap-adb"
  url "https://github.com/spytrap-org/spytrap-adb/releases/download/v0.3.6/spytrap-adb-0.3.6.tar.gz"
  sha256 "8a6e9d43d1495fc052e4724500b43ff19e8bdaa097dde3a716413fe0bb9100fa"
  license "GPL-3.0-or-later"
  head "https://github.com/spytrap-org/spytrap-adb.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "69ca7f42e7288a6a95aa88f8e903ef7e154e507d3a96c081184bc9dc16a0d357"
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
