class Oauth2Proxy < Formula
  desc "Reverse proxy for authenticating users via OAuth 2 providers"
  homepage "https://oauth2-proxy.github.io/oauth2-proxy/"
  url "https://github.com/oauth2-proxy/oauth2-proxy/archive/refs/tags/v7.15.6.tar.gz"
  sha256 "7a24bf731e10d5db9149d7186576bc1286c5d084d3a9536ebece2e53b341b8a9"
  license "MIT"
  head "https://github.com/oauth2-proxy/oauth2-proxy.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0267514566ad86e324b259adca7554f510657a7e2f8ee4d80d4bdcbef786a5dd"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/oauth2-proxy/oauth2-proxy/v7/pkg/version.VERSION=#{version}"
    system "go", "build", *std_go_args(ldflags:, output: bin/"oauth2-proxy")
    (etc/"oauth2-proxy").install "contrib/oauth2-proxy.cfg.example"
    bash_completion.install "contrib/oauth2-proxy_autocomplete.sh" => "oauth2-proxy"
  end

  def caveats
    "#{etc}/oauth2-proxy/oauth2-proxy.cfg must be filled in."
  end

  service do
    run [opt_bin/"oauth2-proxy", "--config=#{etc}/oauth2-proxy/oauth2-proxy.cfg"]
    keep_alive true
    working_dir HOMEBREW_PREFIX
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/oauth2-proxy --version")

    port = free_port
    pid = spawn "#{bin}/oauth2-proxy",
                "--client-id=testing",
                "--client-secret=testing",
                # Cookie secret must be 16, 24, or 32 bytes to create an AES cipher
                "--cookie-secret=0b425616d665d89fb6ee917b7122b5bf",
                "--http-address=127.0.0.1:#{port}",
                "--upstream=file:///tmp",
                "--email-domain=*"

    begin
      output = shell_output("curl --silent --retry 5 --retry-connrefused http://127.0.0.1:#{port}")
      assert_match "<title>Sign In</title>", output
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
