class LttngUst < Formula
  desc "Userspace tracing via ETW TraceLogging (lttng-gen-tp code generator)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "lttng-gen-tp", "lttng-gen-tp"
  end

  test do
    (testpath/"demo.tp").write <<~TP
      TRACEPOINT_EVENT(myapp, greeting, TP_ARGS(int, count, const char*, who),
        TP_FIELDS(ctf_integer(int, count, count) ctf_string(who, who)))
      TP
    system bin/"lttng-gen-tp", testpath/"demo.tp", "-o", testpath/"demo.h", "-o", testpath/"demo.c"
    assert_match "TraceLoggingWrite", (testpath/"demo.h").read
    assert_match "greeting", (testpath/"demo.h").read
    assert_match "TRACELOGGING_DEFINE_PROVIDER", (testpath/"demo.c").read
  end
end
