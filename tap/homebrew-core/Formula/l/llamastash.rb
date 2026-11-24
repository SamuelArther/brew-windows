class Llamastash < Formula
  desc "Terminal-native local LLM orchestrator and proxy"
  homepage "https://github.com/llamastash/llamastash"
  url "https://github.com/llamastash/llamastash/archive/refs/tags/v0.12.1.tar.gz"
  sha256 "62668a7feb4ef361f4754de8bf951b32e78bbe45dec501d07640a97216c68b04"
  license "MIT"
  head "https://github.com/llamastash/llamastash.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(:?\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "13b251a10e3791a815cb56d6f868b84c3b136d6c92daf218101ea52ed063193d"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build
  depends_on "llama.cpp"

  on_linux do
    depends_on "wayland"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    generate_completions_from_executable(bin/"llamastash", "completions")
  end

  service do
    run [opt_bin/"llamastash", "--llama-server", formula_opt_bin("llama.cpp")/"llama-server",
         "daemon", "start", "--foreground"]
    keep_alive true
    log_path var/"log/llamastash.log"
    error_log_path var/"log/llamastash.log"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/llamastash --version")

    output = JSON.parse(shell_output("#{bin}/llamastash knobs --backend llamacpp --json"))
    assert_equal "llamacpp", output.fetch("backends").first.fetch("backend")
    split_mode = output.fetch("backends").first.fetch("knobs").find { |knob| knob["id"] == "split-mode" }
    assert_equal %w[none layer row tensor], split_mode.fetch("choices")
  end
end
