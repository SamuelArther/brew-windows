class Xfr < Formula
  desc "Modern network bandwidth testing with TUI"
  homepage "https://github.com/lance0/xfr"
  url "https://github.com/lance0/xfr/archive/refs/tags/v0.14.1.tar.gz"
  sha256 "fc32357dd5f55d88a6b453b829ff2266efca53871487846c3eb8ae14650a017e"
  license any_of: ["MIT", "Apache-2.0"]
  head "https://github.com/lance0/xfr.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8db50ae59ac233b3653d76c305bef37dfa34028cfced0f682299df9e1e4ca252"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    generate_completions_from_executable(bin/"xfr", shell_parameter_format: "--completions=")
    man1.install "docs/xfr.1"
  end

  test do
    ENV["XFR_NO_UPDATE_CHECK"] = "1"
    port = free_port
    pid = spawn bin/"xfr", "serve", "--port", port.to_s, "--no-mdns"
    sleep 1
    output = shell_output("#{bin}/xfr 127.0.0.1 --port #{port} --time 1sec --json")
    assert_operator JSON.parse(output)["bytes_total"], :>, 0
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
