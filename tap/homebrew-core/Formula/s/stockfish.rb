class Stockfish < Formula
  desc "Strong open-source chess engine"
  homepage "https://stockfishchess.org/"
  license all_of: ["GPL-3.0-only", "CC0-1.0"]
  head "https://github.com/official-stockfish/Stockfish.git", branch: "master"

  stable do
    url "https://github.com/official-stockfish/Stockfish/archive/refs/tags/sf_19.tar.gz"
    sha256 "519b653d0d1ffb96531d982ccbe5c6a19425e8388e0e3c2f70f34b424ab32d76"

    resource "network" do
      url "https://tests.stockfishchess.org/api/nn/nn-1a298aa575a0.nnue"
      version "1a298aa575a0"
      sha256 "1a298aa575a085434d29027978dc36867fe9c5bcea9376654b7a8eba1e52dfc2"

      livecheck do
        # For each update, consult src/evaluate.h for updated network file name
        url "https://raw.githubusercontent.com/official-stockfish/Stockfish/refs/tags/sf_#{LATEST_VERSION}/src/evaluate.h"
        regex(/EvalFileDefaultName\s+"nn-(\h{12})\.nnue"/i)
      end
    end
  end

  livecheck do
    url :stable
    regex(/^sf[._-]v?(\d+(?:\.\d+)*)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1a61bbd0202df5caaa822c7ed7cb1a73a0d919ec8d40e37fe81b74c457b61e02"
  end

  deny_network_access!

  def fetch
    if build.head?
      system "make", "-C", "src", "net"
    else
      resource("network").stage "src"
    end
  end

  def install
    arch = if !build.bottle?
      "native"
    elsif Hardware::CPU.arm? && OS.mac?
      "apple-silicon"
    elsif Hardware::CPU.arm?
      "armv8"
    else
      "x86-64-ssse3"
    end

    args = %W[
      CXX=#{ENV.cxx}
      ARCH=#{arch}
      PREFIX=#{prefix}
    ]

    system "make", "-C", "src", "profile-build", *args
    system "make", "-C", "src", "install", *args
  end

  test do
    system bin/"stockfish", "go", "depth", "20"
  end
end
