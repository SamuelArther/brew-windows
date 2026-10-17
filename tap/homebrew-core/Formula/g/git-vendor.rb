class GitVendor < Formula
  desc "Command for managing git vendored dependencies"
  homepage "https://brettlangdon.github.io/git-vendor"
  url "https://github.com/brettlangdon/git-vendor/archive/refs/tags/v1.3.0.tar.gz"
  sha256 "774c0ba9596f3231c846dad096f61d7e2906f6fad38c031bf6c01bb8d6c0a338"
  license "MIT"
  head "https://github.com/brettlangdon/git-vendor.git", branch: "master"

  allow_network_access! :test

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-vendor-1.3.0"
    sha256 cellar: :any, x86_64_windows: "a7a8a03b743bff927f1e2790b631aa31520931c47380fdb68ac11443121268a7"
  end

  def install
    system "make", "PREFIX=#{prefix}", "install"
  end

  test do
    system "git", "init"
    system "git", "config", "user.email", "author@example.com"
    system "git", "config", "user.name", "Au Thor"
    system "git", "add", "."
    system "git", "commit", "-m", "Initial commit"
    system "git", "vendor", "add", "git-vendor", "https://github.com/brettlangdon/git-vendor", "v1.1.0"
    assert_match "git-vendor@v1.1.0", shell_output("git vendor list")
  end
end
