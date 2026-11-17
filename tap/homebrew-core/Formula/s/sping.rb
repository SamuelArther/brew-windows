class Sping < Formula
  desc "Real-time connectivity monitor with network diagnostics"
  homepage "https://github.com/LambdaBytes/sping"
  url "https://github.com/LambdaBytes/sping/archive/refs/tags/v1.9.0.tar.gz"
  sha256 "e12d45373ca42c3af9adb63370b3a53881aef62bb80990a002cf8da566e957be"
  license "MIT"
  head "https://github.com/LambdaBytes/sping.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e69de212605406f55a900c4d4342977c3d2ba63f136b5ef826fc57a2cb32bd23"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    ENV["SPING_ASSETS_DIR"] = buildpath/"assets"
    system "cargo", "install", *std_cargo_args
    man1.install "assets/man/sping.1"
    bash_completion.install "assets/completions/sping.bash" => "sping"
    fish_completion.install "assets/completions/sping.fish"
    zsh_completion.install "assets/completions/_sping"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sping --version")

    (testpath/"targets").write "# No targets configured\n"
    output = shell_output("#{bin}/sping --batch #{testpath}/targets 2>&1", 2)
    assert_match "contains no targets", output

    output = shell_output("#{bin}/sping 127.0.0.1 --source invalid 2>&1", 2)
    assert_match "invalid source IP address: 'invalid'", output
  end
end
