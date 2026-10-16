class PlzCli < Formula
  desc "Copilot for your terminal"
  homepage "https://github.com/m1guelpf/plz-cli"
  url "https://github.com/m1guelpf/plz-cli/archive/refs/tags/v0.1.9.tar.gz"
  sha256 "ea6b65deb69f86e53210d2457f49a53bb73ab52282fccc5b6cae8e8c059ecb00"
  license "MIT"
  head "https://github.com/m1guelpf/plz-cli.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8b2ac22d8b56c2bd6b038b34deb6232b303729c6ba9af683ff98811db0ef757b"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    ENV["OPENAI_API_KEY"] = "sk-XXXXXXXX"
    assert_match "API error", shell_output("#{bin}/plz brewtest", 1)
  end
end
