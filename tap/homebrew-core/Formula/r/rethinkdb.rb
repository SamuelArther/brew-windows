class Rethinkdb < Formula
  desc "Open-source database for the realtime web"
  homepage "https://rethinkdb.com/"
  # TODO: Check if we can use unversioned `protobuf` at version bump
  # upstream issue report, https://github.com/rethinkdb/rethinkdb/issues/7142
  url "https://download.rethinkdb.com/repository/raw/dist/rethinkdb-2.4.4.tgz"
  sha256 "5091237602b62830db2cb3daaca6ab34632323741e6710c2f0de4d84f442711f"
  license "Apache-2.0"
  head "https://github.com/rethinkdb/rethinkdb.git", branch: "main"

  # There has been no response to Protobuf 25+ issue[^1] opened on 2023-12-13.
  # Upstream appears to be in low maintenance state after parent company shut down[^2].
  # Recently seeing download server issues[^3][^4] which makes source tarball unstable.
  #
  # [^1]: https://github.com/rethinkdb/rethinkdb/issues/7142
  # [^2]: https://github.com/rethinkdb/rethinkdb/issues/6981
  # [^3]: https://github.com/rethinkdb/rethinkdb/issues/7155
  # [^4]: https://github.com/rethinkdb/rethinkdb/issues/7157
  deprecate! date: "2024-11-12", because: "uses unmaintained `protobuf@21`"
  disable! date: "2025-11-12", because: "uses unmaintained `protobuf@21`"

  depends_on "boost" => :build
  depends_on "openssl@3"
  depends_on "protobuf@21"

  uses_from_macos "python" => :build
  uses_from_macos "curl"
  uses_from_macos "ncurses"
  uses_from_macos "zlib"

  def install
    ENV.cxx11
    ENV["PYTHON"] = which("python3")

    args = %W[
      --prefix=#{prefix}
      --sysconfdir=#{etc}
      --localstatedir=#{var}
    ]
    args << "--allow-fetch" if build.head?

    system "./configure", *args
    system "make"
    system "make", "install-binaries"

    inreplace "packaging/assets/config/default.conf.sample",
              /^# directory=.*/, "directory=#{var}/rethinkdb"
    etc.install "packaging/assets/config/default.conf.sample" => "rethinkdb.conf"
  end

  service do
    run [opt_bin/"rethinkdb", "--config-file", etc/"rethinkdb.conf"]
    keep_alive true
    working_dir HOMEBREW_PREFIX
    log_path var/"log/rethinkdb/rethinkdb.log"
    error_log_path var/"log/rethinkdb/rethinkdb.log"
  end

  test do
    shell_output("#{bin}/rethinkdb create -d test")
    assert File.read("test/metadata").start_with?("RethinkDB")
  end
end
