class Tldr < Formula
  desc "Simplified and community-driven man pages"
  homepage "https://tldr.sh/"
  url "https://github.com/tldr-pages/tldr-c-client/archive/refs/tags/v1.6.1.tar.gz"
  sha256 "1a2aa8e764846fad1f41a0304e28416f5c38b6d3a3131ad1e85116749aec34ba"
  license "MIT"
  head "https://github.com/tldr-pages/tldr-c-client.git", branch: "main"

  deprecate! date: "2024-10-24", because: :unmaintained, replacement_formula: "tlrc"
  disable! date: "2025-10-24", because: :unmaintained, replacement_formula: "tlrc"

  depends_on "pkgconf" => :build
  depends_on "libzip"

  uses_from_macos "curl"

  conflicts_with "tlrc", because: "both install `tldr` binaries"
  conflicts_with "tealdeer", because: "both install `tldr` binaries"

  def install
    system "make", "PREFIX=#{prefix}", "install"

    bash_completion.install "autocomplete/complete.bash" => "tldr"
    zsh_completion.install "autocomplete/complete.zsh" => "_tldr"
    fish_completion.install "autocomplete/complete.fish" => "tldr.fish"
  end

  test do
    assert_match "brew", shell_output("#{bin}/tldr brew")
  end
end
