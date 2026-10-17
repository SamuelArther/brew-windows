class Flavours < Formula
  desc "Easy to use base16 scheme manager that integrates with any workflow"
  homepage "https://github.com/Misterio77/flavours"
  url "https://github.com/Misterio77/flavours/archive/refs/tags/v0.7.1.tar.gz"
  sha256 "207002728cc270d92f4cd437384925e767c611d30520ec8a29816dfe600d7f61"
  license "MIT"
  head "https://github.com/Misterio77/flavours.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8caa02b83ec489e46347d3cedbdecdc0c3efe45f404ad35aee357527d2e0f07b"
  end

  depends_on "rust" => :build

  resource "homebrew-testdata", :test do
    url "https://assets2.razerzone.com/images/pnx.assets/618c0b65424070a1017a7168ea1b6337/razer-wallpapers-page-hero-mobile.jpg"
    sha256 "890f0d8fb6ec49ae3b35530a507e54281dd60e5ade5546d7f1d1817934759670"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    resource("homebrew-testdata").stage do
      cmd = "#{bin}/flavours generate --stdout dark razer-wallpapers-page-hero-mobile.jpg"
      expected = /---\n
        scheme:\sGenerated\n
        author:\sFlavours\n
        base00:\s"[0-9a-fA-F]{6}"\n
        base01:\s[0-9a-fA-F]{6}\n
        base02:\s[0-9a-fA-F]{6}\n
        base03:\s[0-9a-fA-F]{6}\n
        base04:\s[0-9a-fA-F]{6}\n
        base05:\s[0-9a-fA-F]{6}\n
        base06:\s[0-9a-fA-F]{6}\n
        base07:\s[0-9a-fA-F]{6}\n
        base08:\s[0-9a-fA-F]{6}\n
        base09:\s[0-9a-fA-F]{6}\n
        base0A:\s[0-9a-fA-F]{6}\n
        base0B:\s[0-9a-fA-F]{6}\n
        base0C:\s[0-9a-fA-F]{6}\n
        base0D:\s[0-9a-fA-F]{6}\n
        base0E:\s[0-9a-fA-F]{6}\n
        base0F:\s[0-9a-fA-F]{6}\n
      /x
      assert_match(expected, shell_output(cmd))
    end
  end
end
