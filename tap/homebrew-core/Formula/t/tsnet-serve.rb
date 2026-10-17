class TsnetServe < Formula
  desc "Expose HTTP applications to a Tailscale Tailnet network"
  homepage "https://github.com/shayne/tsnet-serve"
  url "https://github.com/shayne/tsnet-serve/archive/refs/tags/v1.3.2.tar.gz"
  sha256 "05d11ec7ac4e1bdb2ce6a8db3999e314ceab51ee7b462df3ec75895704438cc7"
  license "MIT"
  head "https://github.com/shayne/tsnet-serve.git", branch: "main"

  deprecate! date: "2026-07-17", because: :repo_archived
  disable! date: "2027-01-17", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "26a340efe980d5dcdb6a16e51628fb202ea2c846a060c0d0f7f2774a2f5de060"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tsnet-serve -version")

    hostname = "test"
    backend = "http://localhost:8080"

    logfile = testpath/"tsnet-serve.log"
    pid = spawn bin/"tsnet-serve", "-hostname", hostname, "-backend", backend,
                out: logfile.to_s, err: logfile.to_s

    sleep 1

    output = logfile.read
    assert_match "tsnet starting with hostname \"#{hostname}\"", output
    assert_match "LocalBackend state is NeedsLogin", output
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
