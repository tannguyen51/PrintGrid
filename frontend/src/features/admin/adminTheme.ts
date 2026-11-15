import { createTheme } from '@mui/material/styles'
import { printGridTheme } from '../../shared/theme/theme'

/** Quiet, compact surfaces scoped to the administration console. */
export const adminTheme = createTheme(printGridTheme, {
  palette: {
    primary: { main: '#6550CE', dark: '#4D38AF', light: '#EEEAFB' },
    background: { default: '#F6F7FB', paper: '#FFFFFF' },
    text: { primary: '#202638', secondary: '#657087' },
    divider: '#E8EBF2', success: { main: '#187653' }, warning: { main: '#996014' },
  },
  shape: { borderRadius: 4 },
  typography: {
    h1: { fontSize: '1.75rem', fontWeight: 750, lineHeight: 1.35, letterSpacing: '-0.04em' },
    h4: { fontSize: '1.75rem', fontWeight: 750, lineHeight: 1.35, letterSpacing: '-0.04em' },
    h6: { fontSize: '1rem', fontWeight: 700 },
    body1: { fontSize: '0.875rem', lineHeight: 1.7 },
    body2: { fontSize: '0.8125rem', lineHeight: 1.65 },
    button: { fontSize: '0.8125rem', letterSpacing: 0 },
  },
  components: {
    MuiPaper: { styleOverrides: { root: { borderRadius: 12, boxShadow: 'none' } } },
    MuiButton: { styleOverrides: {
      root: { borderRadius: 8, whiteSpace: 'nowrap', minHeight: 38 },
      containedPrimary: { background: '#6550CE', boxShadow: '0 2px 3px #231C4C1A', '&:hover': { background: '#4D38AF', boxShadow: 'none' } },
      outlined: { borderColor: '#DCE1EB', '&:hover': { borderColor: '#6550CE', color: '#4D38AF', backgroundColor: '#F5F3FD' } },
      text: { color: '#6550CE', '&:hover': { backgroundColor: '#F5F3FD' } },
      textPrimary: { color: '#6550CE' },
      textError: { color: '#B33232', '&:hover': { backgroundColor: '#FDECEC' } },
    } },
    MuiIconButton: { styleOverrides: { root: { borderRadius: 8 }, colorError: { color: '#B33232' } } },
    MuiAvatar: { styleOverrides: { root: { background: '#EEEAFB', color: '#6550CE' } } },
    MuiOutlinedInput: { styleOverrides: { root: { borderRadius: 8, '&.Mui-focused': { boxShadow: '0 0 0 3px #EEEAFB' }, '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: '#6550CE' } } } },
    MuiChip: { styleOverrides: {
      root: { fontSize: '0.7rem', fontWeight: 600 },
      colorPrimary: { color: '#5540B7', backgroundColor: '#F1EDFC', borderColor: '#E4DCF8' },
      colorSuccess: { color: '#187653', backgroundColor: '#ECF8F1', borderColor: '#D2EDDF' },
      colorWarning: { color: '#996014', backgroundColor: '#FFF7E8', borderColor: '#F4E4C4' },
    } },
    MuiTabs: { styleOverrides: { indicator: { height: 2, background: '#6550CE' } } },
    MuiTab: { styleOverrides: { root: { '&.Mui-selected': { color: '#5540B7' } } } },
    MuiTableCell: { styleOverrides: { head: { textTransform: 'none', letterSpacing: 0, fontSize: '0.75rem', backgroundColor: '#F9FAFC' } } },
    MuiDataGrid: {
      defaultProps: { localeText: {
        noRowsLabel: 'Chưa có dữ liệu', noResultsOverlayLabel: 'Không có kết quả phù hợp',
        footerRowSelected: (count: number) => `${count} dòng được chọn`,
        paginationRowsPerPage: 'Số dòng mỗi trang:',
        paginationDisplayedRows: ({ from, to, count }: { from: number; to: number; count: number }) => `${from}–${to} / ${count === -1 ? `hơn ${to}` : count}`,
      } },
      styleOverrides: {
        root: { fontSize: '0.8125rem', '--DataGrid-containerBackground': '#F9FAFC', '--DataGrid-rowBorderColor': '#EFF1F5' },
        columnHeader: { textTransform: 'none', letterSpacing: 0, fontSize: '0.75rem', '&:focus, &:focus-within': { outline: '2px solid #6550CE', outlineOffset: -2 } },
        cell: { '&:focus, &:focus-within': { outline: '2px solid #6550CE', outlineOffset: -2 } },
      },
    },
  },
})
