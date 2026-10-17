class GitPages < Formula
  desc "Scalable static site server for Git forges"
  homepage "https://codeberg.org/git-pages/git-pages"
  url "https://codeberg.org/git-pages/git-pages/releases/download/v0.9.1/git-pages-src.zip"
  sha256 "f9c71aa5be211cbd7e03637ec7455e124e05e5cabb040f09e05e188358c2ea00"
  license "0BSD"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2d71f8777e8567b2df09a043506b4e958763fe2373b35bb751de6b4fab09701a"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    mkdir_p "data"
    port = free_port

    (testpath/"config.toml").write <<~TOML
      [server]
      pages = "tcp/localhost:#{port}"
    TOML

    ENV["PAGES_INSECURE"] = "1"

    spawn bin/"git-pages"

    sleep 2
    system "curl", "http://127.0.0.1:#{port}", "-X", "PUT", "-d", "https://codeberg.org/git-pages/git-pages.git"

    sleep 2
    assert_equal "It works!\n", shell_output("curl http://127.0.0.1:#{port}")
  end
end
