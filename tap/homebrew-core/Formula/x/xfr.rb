class Xfr < Formula
  desc "Modern network bandwidth testing with TUI"
  homepage "https://github.com/lance0/xfr"
  url "https://github.com/lance0/xfr/archive/refs/tags/v0.12.1.tar.gz"
  sha256 "58f525cf039f405ea976b412b720a757867523755fd6c8a3e73e08fb98b35147"
  license any_of: ["MIT", "Apache-2.0"]
  head "https://github.com/lance0/xfr.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5465984a0a89026237a2974caeeff7476a18a8ec6a762acba96305dbdc62b63c"
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
