class GitGrab < Formula
  desc "Clone a git repository into a standard location organised by domain and path"
  homepage "https://codeberg.org/wezm/git-grab"
  url "https://static.crates.io/crates/git-grab/git-grab-4.0.1.crate"
  sha256 "5182ee527d93f15e811d346fb86f3e4c2c24fbc479d4deb57efda6bec065a1c0"
  license any_of: ["Apache-2.0", "MIT"]

  bottle do
    sha256 cellar: :any, x86_64_windows: "31b43328e8f6dbe48bd6a453ce4b528a81e504d704950a5a96259ac056faf997"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    system "git", "grab", "--home", testpath, "https://codeberg.org/wezm/git-grab.git"
    assert_path_exists testpath/"codeberg.org/wezm/git-grab/Cargo.toml"

    assert_match "git-grab version #{version}", shell_output("#{bin}/git-grab --version")
  end
end
