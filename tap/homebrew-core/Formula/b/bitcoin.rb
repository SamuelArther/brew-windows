class Bitcoin < Formula
  desc "Decentralized, peer to peer payment network"
  homepage "https://bitcoincore.org/"
  url "https://bitcoincore.org/bin/bitcoin-core-32.1/bitcoin-32.1.tar.gz"
  sha256 "a5f2f61e32cf7f790641c9ec306a3c88ca7b5a556946956b1f35ffd7e27813f9"
  license all_of: [
    "MIT",
    "BSD-3-Clause", # src/crc32c, src/leveldb
    "BSL-1.0", # src/tinyformat.h
  ]
  compatibility_version 1
  head "https://github.com/bitcoin/bitcoin.git", branch: "master"

  livecheck do
    url "https://bitcoincore.org/en/download/"
    regex(/latest version.*?v?(\d+(?:\.\d+)+)/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "bf87282c163532abb01a65061516daf912a6045de68592138c8a0631bd7788f0"
  end

  depends_on "boost" => :build
  depends_on "cmake" => :build
  depends_on "pkgconf" => :build
  depends_on "capnp"
  depends_on "libevent"
  depends_on "zeromq"

  uses_from_macos "sqlite"

  on_macos do
    depends_on macos: :sonoma # Needs C++20 features not available on Ventura
  end

  fails_with :gcc do
    version "11"
    cause "Requires C++ 20"
  end

  deny_network_access!

  def install
    ENV.runtime_cpu_detection
    args = %w[
      -DWITH_ZMQ=ON
    ]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
    pkgshare.install "share/rpcauth"
  end

  service do
    run opt_bin/"bitcoind"
  end

  test do
    ENV["TMPDIR"] = testpath

    # sock_tests bind TCP sockets on loopback, which the network sandbox denies
    system bin/"bitcoin", "test", "--run_test=!sock_tests"
  end
end
