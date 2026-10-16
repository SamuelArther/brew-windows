class Rainfrog < Formula
  desc "Database management TUI for PostgreSQL/MySQL/SQLite"
  homepage "https://github.com/achristmascarl/rainfrog"
  url "https://github.com/achristmascarl/rainfrog/archive/refs/tags/v0.4.6.tar.gz"
  sha256 "30d4c4843c040441422eb9d64d386bf35f6c2c1de345944cc3e12f17e0cd0ca5"
  license "MIT"
  head "https://github.com/achristmascarl/rainfrog.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f9f4ebdb4e7f30310cead61fa32b84a212be03d729bbafcdbfa5d68d719d8648"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    # rainfrog is a TUI application
    assert_match version.to_s, shell_output("#{bin}/rainfrog --version")
  end
end
