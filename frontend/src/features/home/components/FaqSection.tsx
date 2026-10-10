import { Box, Container, Typography, Accordion, AccordionSummary, AccordionDetails } from '@mui/material';
import { ExpandMore } from '@mui/icons-material';

const FAQS = [
  { q: 'PrintGrid hỗ trợ định dạng file nào?', a: 'Chúng tôi hỗ trợ STL, OBJ, 3MF và GLB. Dung lượng tối đa 50 MB cho mỗi file.' },
  { q: 'Báo giá có hiệu lực trong bao lâu?', a: 'Báo giá có hiệu lực trong vòng 48 giờ kể từ khi được tạo.' },
  { q: 'Làm sao tôi biết xưởng nào đang in sản phẩm của mình?', a: 'PrintGrid phân bổ đơn hàng tối ưu dựa trên năng lực mạng lưới. Bạn không cần bận tâm xưởng nào in, mọi sản phẩm đều được kiểm tra chất lượng nghiêm ngặt tại Hub trước khi giao.' },
  { q: 'Chính sách bảo hành như thế nào?', a: 'Chúng tôi bảo hành 30 ngày kể từ khi giao hàng. Nếu sản phẩm có lỗi từ nhà sản xuất, bạn có thể gửi yêu cầu in lại hoàn toàn miễn phí.' },
];

export function FaqSection() {
  return (
    <Box sx={{ py: 8 }}>
      <Container maxWidth="md">
        <Typography variant="h4" textAlign="center" fontWeight={700} mb={6}>Câu hỏi thường gặp</Typography>
        {FAQS.map((faq, idx) => (
          <Accordion key={idx} sx={{ bgcolor: 'background.paper', mb: 1, '&:before': { display: 'none' } }} variant="outlined">
            <AccordionSummary expandIcon={<ExpandMore />}>
              <Typography fontWeight={600}>{faq.q}</Typography>
            </AccordionSummary>
            <AccordionDetails>
              <Typography color="text.secondary">{faq.a}</Typography>
            </AccordionDetails>
          </Accordion>
        ))}
      </Container>
    </Box>
  );
}
