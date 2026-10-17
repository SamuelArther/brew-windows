class GitSecrets < Formula
  desc "Prevents you from committing sensitive information to a git repo"
  homepage "https://github.com/awslabs/git-secrets"
  license "Apache-2.0"
  head "https://github.com/awslabs/git-secrets.git", branch: "master"

  stable do
    url "https://github.com/awslabs/git-secrets/archive/refs/tags/1.3.0.tar.gz"
    sha256 "f1d50c6c5c7564f460ff8d279081879914abe920415c2923934c1f1d1fac3606"

    # Backport removal of unnecessary dependency on `say`
    patch do
      url "https://github.com/awslabs/git-secrets/commit/65891e23f341f159098300999edcce5983cd3ad8.patch?full_index=1"
      sha256 "add9ad9f5778dd38885a23b8b394601061a203d1862b91cd64c5ca2a0c9a6ab2"
      type :backport
      resolves "https://github.com/awslabs/git-secrets/pull/221"
    end
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-secrets-1.3.0"
    sha256 cellar: :any, x86_64_windows: "ded481d4a54475ed95ea0d38aab620d60821e1b60033d622d1cefc219b59826b"
  end

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    system "git", "init"
    system "git", "secrets", "--install"
  end
end
