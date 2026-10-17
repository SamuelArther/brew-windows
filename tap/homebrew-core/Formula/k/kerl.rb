class Kerl < Formula
  desc "Easy building and installing of Erlang/OTP instances"
  homepage "https://github.com/kerl/kerl"
  url "https://github.com/kerl/kerl/archive/refs/tags/4.4.0.tar.gz"
  sha256 "0f32eb08172baffdca9264c5626f6d7fd650369365079fe21f8b8ab997885d8c"
  license "MIT"
  head "https://github.com/kerl/kerl.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/kerl-4.4.0"
    sha256 cellar: :any, x86_64_windows: "51a1b8784c7d145dfe47a07550991aba237eee9a09b41415c29656f5ac25656a"
  end

  def install
    bin.install "kerl"

    bash_completion.install "bash_completion/kerl"
    zsh_completion.install "zsh_completion/_kerl"
    fish_completion.install "fish_completion/kerl.fish"
  end

  test do
    system bin/"kerl", "list", "releases"
  end
end
