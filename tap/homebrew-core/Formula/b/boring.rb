class Boring < Formula
  desc "Simple command-line SSH tunnel manager that just works"
  homepage "https://alebeck.github.io/boring/"
  url "https://github.com/alebeck/boring/archive/refs/tags/v0.18.1.tar.gz"
  sha256 "a847e3fbd99eaf82c5a3386c51021a6b93c0b74eb9f51ebbfac8e2bca9547a16"
  license "MIT"
  head "https://github.com/alebeck/boring.git", branch: "main"

  no_autobump! because: :bumped_by_upstream

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "363082253654e074209cbf3642e820e96f6cfd41cd15b7dbf58232cf4c110c7f"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/alebeck/boring/internal/buildinfo.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:), "./cmd/boring"

    generate_completions_from_executable(bin/"boring", "--shell")
  end

  post_install_steps do
    terminate_process "boring", must_succeed: false
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/boring version")

    (testpath/(OS.linux? ? ".config/boring" : "")/".boring.toml").write <<~TOML
      [[tunnels]]
      name = "dev"
      local = "9000"
      remote = "localhost:9000"
      host = "dev-server"
    TOML

    # Keep the daemon socket inside testpath, the only place the test sandbox allows unix sockets
    ENV["BORING_SOCK"] = testpath/"boringd.sock"
    assert_match "dev   9000   ->  localhost:9000  dev-server", shell_output("#{bin}/boring list")
  end
end
