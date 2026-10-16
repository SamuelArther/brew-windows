class BrewCaskCompletion < Formula
  desc "Fish completion for brew-cask"
  homepage "https://github.com/xyb/homebrew-cask-completion"
  url "https://github.com/xyb/homebrew-cask-completion/archive/refs/tags/v2.1.tar.gz"
  sha256 "27c7ea3b7f7c060f5b5676a419220c4ce6ebf384237e859a61c346f61c8f7a1b"
  license "BSD-2-Clause"
  revision 1
  head "https://github.com/xyb/homebrew-cask-completion.git", branch: "master"

  deprecate! date: "2025-05-17", because: "is now natively supported by `brew`"
  disable! date: "2026-05-17", because: "is now natively supported by `brew`"

  def install
    fish_completion.install "brew-cask.fish"
  end

  test do
    assert_path_exists fish_completion/"brew-cask.fish"
  end
end
